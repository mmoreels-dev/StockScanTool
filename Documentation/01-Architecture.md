# Architecture Overview

## Clean Architecture

StockScanTool follows **Clean Architecture** (also known as Onion/Hexagonal Architecture). Dependencies flow inward — the Domain has zero dependencies, and outer layers depend only on inner layers.

```
┌─────────────────────────────────────────────────────────────┐
│                      OUTER LAYER                            │
│                                                             │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐   │
│  │   Api         │  │   Web         │  │  ScannerPwa   │   │
│  │   (HTTP       │  │   (Blazor     │  │  (Blazor      │   │
│  │   Controllers)│  │   WASM SPA)   │  │   WASM PWA)   │   │
│  └───────┬───────┘  └───────┬───────┘  └───────┬───────┘   │
│          │                  │                   │           │
│  ┌───────┴──────────────────┴───────────────────┴───────┐   │
│  │              Infrastructure                          │   │
│  │  EF Core DbContext, Repositories, Services, JWT      │   │
│  └───────────────────────┬──────────────────────────────┘   │
│                          │                                  │
│  ┌───────────────────────┴──────────────────────────────┐   │
│  │              Application                             │   │
│  │  Service interfaces, Repository interfaces, Result   │   │
│  └───────────────────────┬──────────────────────────────┘   │
│                          │                                  │
│  ┌───────────────────────┴──────────────────────────────┐   │
│  │              Contracts                               │   │
│  │  DTOs, Validators, API Routes                        │   │
│  └───────────────────────┬──────────────────────────────┘   │
│                          │                                  │
│  ┌───────────────────────┴──────────────────────────────┐   │
│  │              Domain                                  │   │
│  │  Entities, Value Objects (zero dependencies)         │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                             │
│                      INNER LAYER                            │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Rules

```
Domain          ← no dependencies (pure entities)
    ↑
Contracts       ← depends on Domain (DTOs reference entity shapes)
    ↑
Application     ← depends on Contracts (interfaces use DTOs)
    ↑
Infrastructure  ← depends on Application + Contracts (implements interfaces)
    ↑
Api / Web / ScannerPwa  ← depends on Infrastructure + Contracts (entry points)
```

## Project Descriptions

### Domain Layer (`StockScanTool.Domain`)

The innermost layer. Contains only entity classes and the `AuditableEntity` base class. No external dependencies.

**Entities:** `Store`, `Product`, `Inventory`, `SaleTransaction`, `SaleItem`, `ScanningDevice`, `User`, `Role`, `Permission`, `UserRole`, `RolePermission`

### Contracts Layer (`StockScanTool.Contracts`)

Shared DTOs, request/response records, API route constants, and FluentValidation validators. Referenced by both the API and all client applications.

**Key files:**
- `Common/ApiRoutes.cs` — Centralized route constants
- `Common/ApiResponse.cs` — Standard API response envelope
- `Common/PagedRequest.cs` / `PagedResult.cs` — Pagination support
- `Validators/` — FluentValidation rule sets

### Application Layer (`StockScanTool.Application`)

Defines interfaces for services and repositories. Contains the `Result<T>` monad for domain-level success/failure. No implementations.

**Key interfaces:**
- `IProductService`, `IStoreService`, `IDeviceService`, `IInventoryService`, `ISaleService`, `IDashboardService`
- `IUserService`, `IRoleService`, `IPermissionService` — RBAC management
- `IRepository<T>`, `IProductRepository`, `IStoreRepository`, `ISaleTransactionRepository` (with `GetByIdWithIncludesAsync` for eager loading)
- `IUnitOfWork` — Transaction management
- `IJwtTokenService` — Token generation

### Infrastructure Layer (`StockScanTool.Infrastructure`)

Implements all Application interfaces. Contains EF Core DbContext, repository implementations, service logic, database migrations, seed data, and DI registration.

**Key components:**
- `AppDbContext` — EF Core context with Fluent API configurations
- `Repository<T>` — Generic repository pattern
- `CrudService<T>` — Generic CRUD operations
- `EntityMapper` — Entity-to-DTO mapping
- `AuthService` / `JwtTokenService` — Authentication logic (DB-backed user auth + device auth)
- `PasswordHasher` — PBKDF2 password hashing
- `ApiKeyHasher` — SHA-256 API key hashing
- `DependencyInjection.cs` — `AddInfrastructure()` extension method

### API Layer (`StockScanTool.Api`)

ASP.NET Core Web API hosting controllers, middleware, Swagger/OpenAPI, CORS, rate limiting, and JWT authentication.

**Entry point:** `Program.cs` configures Kestrel, JWT, CORS, rate limiting, and the middleware pipeline.

**Controllers:** `AuthController`, `StoresController`, `ProductsController`, `DevicesController`, `InventoryController`, `SalesController`, `DashboardController`, `UsersController`, `RolesController`, `PermissionsController`

**Authorization:** `[HasPermission("module.action")]` attribute via `PermissionAuthorizationHandler`, with 26 registered policies in `Program.cs`.

### Web Layer (`StockScanTool.Web`)

Blazor WebAssembly standalone application using MudBlazor for the admin dashboard. Communicates with the API via HTTP.

**Key services:**
- `ApiClient` — HTTP client with JWT token management and 401 auto-logout
- `AuthStateService` — localStorage-based authentication state

### Scanner Client (`StockScanTool.ScannerPwa`)

Blazor WebAssembly PWA for barcode scanning. Uses MudBlazor UI (matching the Web admin dashboard theme). Uses the native `BarcodeDetector` API (Chromium) with a bundled `html5-qrcode` fallback for camera-based barcode reading. Service worker enables offline support.

### Shared Client Library (`StockScanTool.Shared`)

Shared services used by both scanner clients: `BarcodeScannerService`, `DeviceAuthService`, `ProductLookupService`, `SaleSubmissionService`, `BaseApiService`. The `BarcodeScannerService` provides C# JS interop (via the consolidated `scanner.js`) for both continuous scanning (PWA) and single-scan (Admin dialog) modes.

## Data Flow

### Admin Dashboard Data Flow

```
┌──────────────┐     HTTP GET      ┌──────────────┐     EF Core     ┌──────────┐
│  MudDataGrid │ ────────────────→ │  REST API    │ ──────────────→ │  SQLite  │
│  (Browser)   │ ←──────────────── │  Controller  │ ←────────────── │  DB      │
└──────────────┘   JSON Response   └──────────────┘   Entity Query  └──────────┘
       ↑                                  ↑
       │                                  │
  ApiClient.GetProducts()          ProductsController.GetAll()
  (attaches Bearer token)          [Authorize] attribute
```

### Scanner Sale Flow

```
┌──────────────┐   POST /sales    ┌──────────────┐   Stock Check   ┌──────────┐
│  Scanner     │ ───────────────→ │  SalesAPI    │ ──────────────→ │ Inventory│
│  (PWA/MAUI)  │ ←──────────────── │  Controller  │ ←────────────── │ Table    │
└──────────────┘   200 OK / Error  └──────────────┘   Decrement     └──────────┘
       ↑                                  ↑
       │                                  │
  Device auth via                   Validate stock,
  API key → JWT                     create SaleTransaction
```

## Error Handling

The API uses a global `ExceptionHandlingMiddleware` that catches unhandled exceptions and returns standardized `ProblemDetails` responses:

```
┌──────────────────────────┬────────────────────────────────┐
│ Exception Type           │ HTTP Response                   │
├──────────────────────────┼────────────────────────────────┤
│ ValidationException      │ 400 Bad Request                │
│ ArgumentException        │ 400 Bad Request                │
│ KeyNotFoundException     │ 404 Not Found                  │
│ UnauthorizedAccessException │ 401 Unauthorized            │
│ InvalidOperationException │ 409 Conflict                   │
│ (any other)              │ 500 Internal Server Error      │
└──────────────────────────┴────────────────────────────────┘
```

## Response Envelope

All API responses are wrapped in a standard envelope:

```json
{
  "success": true,
  "data": { ... },
  "error": null,
  "errors": null
}
```

On failure:

```json
{
  "success": false,
  "data": null,
  "error": "Product not found.",
  "errors": ["Additional error details"]
}
```
