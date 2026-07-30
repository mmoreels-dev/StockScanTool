# AI Agent Memory File

This file tracks all changes made by AI models and agents in this repository. Each session is logged with date, model, scope, and a summary of modifications. Future agents should read this file first to understand the project's current state and history.

---

## Session Log

---

### Session 1 — Blazor WASM Rebuild + MudBlazor Migration

- **Date:** 2026-07-27
- **Model:** opencode/big-pickle
- **User Request:** "Analyze the web application. i want a clean way to show the data or a complete rebuild of the web application."
- **Decision:** Full rebuild of `StockScanTool.Web` from Blazor Server to Blazor WebAssembly Standalone with MudBlazor UI framework.

#### Changes Made

**Project Conversion (StockScanTool.Web.csproj):**
- Changed SDK from `Microsoft.NET.Sdk.Web` → `Microsoft.NET.Sdk.BlazorWebAssembly`
- Added NuGet packages: `MudBlazor 7.*`, `Microsoft.AspNetCore.Components.WebAssembly 10.*`, `Microsoft.AspNetCore.Components.WebAssembly.DevServer 10.*`
- Removed `StockScanTool.Shared` project reference (server-side dependencies not compatible with WASM)
- Kept `StockScanTool.Contracts` reference

**Program.cs:**
- Rewritten from ASP.NET Core host to `WebAssemblyHostBuilder`
- Removed Kestrel/HTTPS configuration (WASM runs in browser)
- Removed `AddRazorComponents().AddInteractiveServerComponents()`
- Added `AddMudServices()`
- Registered `HttpClient`, `AuthStateService`, `ApiClient` as scoped services

**wwwroot/index.html:**
- Replaced `blazor.web.js` with `blazor.webassembly.js`
- Added MudBlazor CSS (`_content/MudBlazor/MudBlazor.min.css`) and JS (`_content/MudBlazor/MudBlazor.min.js`)
- Added Google Fonts (Inter)
- Added loading splash screen

**wwwroot/css/app.css:**
- Created new CSS with custom properties (`--sst-primary`, `--sst-secondary`, etc.)
- Added MudBlazor theme overrides (appbar border, nav-link active states, data grid headers, dialog titles)
- Added stock level row color classes (`.row-danger`, `.row-warning`, `.row-ok`)

**wwwroot/js/auth.js:**
- Rewritten to call API directly at `/api/v1/Auth/admin-login` (removed server-side proxy)
- Changed token storage from `sessionStorage` to `localStorage` (persists across sessions)
- Updated response parsing to unwrap `body.data.token` from `ApiResponse<T>` envelope

**Components/App.razor:**
- Rewritten for WASM (references `blazor.webassembly.js` instead of `blazor.web.js`)

**Components/Routes.razor:**
- Rewritten for WASM (uses `typeof(App).Assembly` instead of `typeof(Program).Assembly`)

**Components/_Imports.razor:**
- Added `@using MudBlazor`

**Layout/MainLayout.razor:**
- Rewritten with `MudLayout`, `MudAppBar` (black bg, orange accent), `MudDrawer` (dark sidebar, orange active nav links)
- MudBlazor theme configured with custom palette (orange primary, black secondary, green success, etc.)
- Navigation: Dashboard, Stores (NEW), Products, Devices, Inventory, Sales
- Logout button in AppBar

**Layout/LoginLayout.razor:**
- Rewritten with `MudLayout` centered on dark background

**Services/AuthStateService.cs:**
- **New file.** Singleton tracking `IsAuthenticated`, `Token`, `LoginAsync()`, `LogoutAsync()`, `CheckAuthAsync()`
- Uses JS interop to read/write `localStorage`

**Services/ApiClient.cs:**
- Rewritten to remove `IJSRuntime` dependency (uses `AuthStateService` instead)
- Added centralized 401 detection → auto-logout → redirect to `/login`
- Unwraps `ApiResponse<T>.Data` automatically in all HTTP helpers
- All store/product/device/inventory/sales/dashboard methods preserved

**Pages/Login/LoginPage.razor:**
- Rewritten with `MudCard`, `MudTextField` inputs, `MudButton` with loading spinner
- Orange branding header with logo icon
- `MudSnackbar` feedback

**Pages/Dashboard/DashboardPage.razor:**
- Rewritten with `MudCard` stat cards (stores, products, devices, revenue)
- `MudChart` (Donut) for Sales by Store
- `MudChart` (Bar) for Stock Levels
- `MudDataGrid` for both tables with conditional row styling
- Removed old scoped CSS file

**Pages/Stores/StoresPage.razor:**
- **New file.** Full CRUD with `MudDataGrid`, search, store filter, add/edit/delete
- Uses `MudDialog` for Add/Edit form

**Pages/Stores/StoreDialog.razor:**
- **New file.** `MudDialog` with name, address, active toggle fields

**Pages/Products/ProductsPage.razor:**
- Rewritten with `MudDataGrid`, search, barcode chip display
- `MudDialog` for Add/Edit with barcode scanner integration

**Pages/Products/ProductDialog.razor:**
- Rewritten with `MudDialog`, `MudNumericField` for price, barcode scan button
- JS interop for `adminScanner.scanOnce()` via `DotNetObjectReference`

**Pages/Devices/DevicesPage.razor:**
- Rewritten with `MudDataGrid`, status chips (Active/Inactive), store filter
- Removed API key display (was showing hashed value — a security/usability bug)

**Pages/Devices/DeviceDialog.razor:**
- Rewritten with `MudDialog`, `MudSelect` for store

**Pages/Inventory/InventoryPage.razor:**
- Rewritten with `MudDataGrid`, store filter, search, color-coded quantity chips
- Conditional row styling (red=0, yellow=≤5, green=ok)

**Pages/Inventory/InventoryDialog.razor:**
- Rewritten with `MudDialog`, store/product selects, numeric quantity

**Pages/Sales/SalesPage.razor:**
- Rewritten with `MudDataGrid`, store filter, expandable transaction detail
- Click row to view sale items
- Summary line showing count and total

**Pages/Error.razor:**
- Rewritten for WASM (removed `HttpContext` dependency)

**Properties/launchSettings.json:**
- **New file.** Configured WASM dev server on ports 5000/5001

**API Changes:**
- `ProductsController.cs`: Added `[Authorize]` attribute (was unprotected)
- `StoresController.cs`: Added `[Authorize]` attribute (was unprotected)
- `appsettings.json`: Added `https://localhost:5001` to CORS allowed origins

**Deleted Files:**
- `wwwroot/app.css` (old, replaced by `wwwroot/css/app.css`)
- `Components/Layout/MainLayout.razor.css` (scoped CSS, replaced by MudBlazor theme)
- `Components/Pages/Dashboard/DashboardPage.razor.css` (scoped CSS)
- `Components/Pages/Devices/DevicesPage.razor.css` (scoped CSS)
- `Components/Layout/AuthenticatedPageBase.razor` (replaced by `AuthStateService` pattern)

---

### Session 2 — Project Documentation

- **Date:** 2026-07-27
- **Model:** opencode/big-pickle
- **User Request:** "create documentation for the project and put the files in the documentation folder"
- **Decision:** Full documentation suite with ASCII diagrams for developers.

#### Files Created

**README.md (rewritten):**
- Modern project overview with architecture diagram
- Tech stack table, quick start commands, project list
- Links to all documentation files

**Documentation/00-INDEX.md:**
- Documentation landing page with table of contents

**Documentation/01-Architecture.md:**
- Clean Architecture layer diagram and dependency rules
- All 8 project descriptions with key files
- Data flow diagrams (admin dashboard, scanner sale)
- Error handling matrix (exception type → HTTP status)
- Response envelope format

**Documentation/02-Getting-Started.md:**
- Prerequisites, first-time setup, build commands
- Running API (port 5168), Web (port 5001), Scanner PWA
- Database setup (SQLite default, SQL Server option)
- Seed data (6 stores), TLS certificates, troubleshooting

**Documentation/03-API-Reference.md:**
- All 23 API endpoints documented with:
  - HTTP method and full route
  - Auth requirement (Admin/Device/Anonymous)
  - Request body JSON examples
  - Response JSON examples
  - curl examples
  - Query parameters where applicable

**Documentation/04-Database-Schema.md:**
- ASCII Entity Relationship Diagram
- Cardinality summary
- All 6 entity field tables with types, constraints, descriptions
- Unique index on `(ProductId, StoreId)` in Inventory
- Seed data table
- Database provider configuration (SQLite vs SQL Server)
- Migration commands

**Documentation/05-Authentication.md:**
- Admin login flow diagram (sequence diagram)
- Device login flow diagram (sequence diagram)
- JWT token structure (Admin claims, Device claims)
- Token settings table
- Role-based access control matrix (all endpoints × roles)
- Token lifecycle flowchart
- 401 handling process
- API key security (SHA-256 hashing, one-time visibility)
- Rate limiting configuration
- Production security recommendations

**Documentation/06-Configuration.md:**
- All API config keys with types, defaults, descriptions
- All Web config keys
- Environment variable overrides
- Certificate file reference
- Configuration hierarchy
- Full config file listings

**Documentation/07-Development-Guide.md:**
- Code style conventions (.editorconfig summary)
- C# and Razor conventions
- Adding new entities (10-step guide)
- Adding new API endpoints (7-step guide)
- Test project structure and commands
- Test coverage table
- Branch strategy diagram (main/dev/test/feature/fix/release)
- Contributing workflow
- Useful commands reference
- IDE setup for VS, VS Code, Rider

---

### Session 3 — Bug Fixes: Sale Crash, Search, Device Toggle, Currency, Auth Header

- **Date:** 2026-07-28
- **Model:** opencode/big-pickle
- **User Request:** "review this project, debug and do not guess, come up with real info to prove issues... fix all step by step"
- **Decision:** Fix all 8 verified bugs across the codebase, from critical NullReferenceException to dead code removal.

#### Bugs Fixed

**Bug #1 (CRITICAL) — SaleService NullReferenceException (`SaleService.cs:112`, `EntityMapper.cs:21-29`):**
- After `SubmitSaleAsync` saved a `SaleTransaction`, it called `EntityMapper.ToDto(transaction)` which accessed `t.Store.Name`, `t.ScanningDevice.DeviceName`, and `t.SaleItems[].Product.Name` — all null because navigation properties were never loaded.
- **Fix:** Added `GetByIdWithIncludesAsync(int id)` to `ISaleTransactionRepository` interface and `SaleTransactionRepository` implementation with `.Include()` for Store, ScanningDevice, and SaleItems+Product. `SaleService` now re-queries the saved transaction after `SaveChangesAsync`.
- **Files:** `ISaleTransactionRepository.cs`, `SaleTransactionRepository.cs`, `SaleService.cs`

**Bug #2 (MEDIUM) — Store search filter dead code (`StoresPage.razor:30`):**
- `MudDataGrid` bound to `@stores` (unfiltered) instead of `@FilteredStores`, making the search text box non-functional.
- **Fix:** Changed `Items="@stores"` to `Items="@FilteredStores"`.
- **File:** `StoresPage.razor`

**Bug #3 (MEDIUM) — DeviceDialog isActive hardcode (`DeviceDialog.razor:64`):**
- Edit dialog hardcoded `isActive: true` in `UpdateDeviceRequest`, meaning devices could never be deactivated and editing always reactivated them.
- **Fix:** Added `isActive` field initialized from `Device.IsActive`, added `MudSwitch` UI toggle (shown only on edit), used actual value in save call.
- **File:** `DeviceDialog.razor`

**Bug #4 (MEDIUM) — PWA Scanner currency format (`ScannerPage.razor:52,98`):**
- Razor markup `@cart.Sum(c => c.Total):C` rendered literal `:C` instead of currency formatting (e.g. `$123.45:C`).
- **Fix:** Changed to `@cart.Sum(c => c.Total).ToString("C")`.
- **File:** `ScannerPage.razor` (ScannerPwa)

**Bug #5 (LOW) — Stale auth header after logout (`ApiClient.cs:35-47`):**
- After logout, `_http.DefaultRequestHeaders.Authorization` was never cleared, leaving the old token header on the HttpClient.
- **Fix:** Added `else` branch in `EnsureTokenAsync()` to clear `Authorization` header and reset `_tokenAttached` when not authenticated.
- **File:** `ApiClient.cs`

**Bug #6 (LOW) — Admin barcode scanner uses older API (`admin-scanner.js`):**
- Only used `Html5QrcodeScanner` (older API with worse camera rendering). Main `barcode-scanner.js` preferred `Html5Qrcode` (newer direct API).
- **Fix:** Rewrote to prefer `Html5Qrcode` with proper error handling, matching `barcode-scanner.js` pattern.
- **File:** `admin-scanner.js`

**Bug #7 (LOW) — Admin settings hash ordering (`Program.cs:42-53`):**
- `Configure<AdminSettings>()` was called before the password hash was generated and written to configuration, relying on lazy `IOptions` resolution.
- **Fix:** Moved `Configure<AdminSettings>()` after the hash generation block.
- **File:** `Program.cs` (Api)

**Bug #8 (LOW) — Dead DataAnnotationsValidator (`LoginPage.razor:20`):**
- `<DataAnnotationsValidator />` with `Model="this"` did nothing — no `[Required]` attributes on the component. Actual validation was manual in `HandleLogin()`.
- **Fix:** Removed the dead `<DataAnnotationsValidator />` line.
- **File:** `LoginPage.razor` (Web)

**Test Fix:**
- Added mock setup for `GetByIdWithIncludesAsync` in `SaleServiceTests.SubmitSaleAsync_ReturnsSuccess_WhenValidSale` to match the new repository interface.
- **File:** `SaleServiceTests.cs`

#### Verification

- Build: 0 errors, 24 pre-existing warnings (CS4014, MUD0002)
- Tests: 62/62 pass (55 unit + 7 integration)

---

### Session 4 — PWA MudBlazor Alignment + MM Branding

- **Date:** 2026-07-28
- **Model:** opencode/big-pickle
- **User Request:** "UI needs to be alined between the web and the wpa project, the wpa should look like the web UI"
- **Decision:** Added MudBlazor to `StockScanTool.ScannerPwa`, matching the Web project's theme, component usage, and visual design language.

#### Changes Made

**New Files:**
- `Components/Pages/CartDialog.razor` — MudBlazor dialog for shopping cart with MudTable, qty +/- buttons, remove, checkout
- `wwwroot/media/logo.svg` — Orange rounded-square "MM" logo (brand identity)

**StockScanTool.ScannerPwa.csproj:**
- Added `MudBlazor 7.*` package reference

**Program.cs:**
- Added `using MudBlazor.Services;`
- Added `builder.Services.AddMudServices();`

**wwwroot/index.html:**
- Added Inter font (Google Fonts) — matches Web project
- Added MudBlazor CSS (`_content/MudBlazor/MudBlazor.min.css`) and JS (`_content/MudBlazor/MudBlazor.min.js`)
- Replaced SVG loading spinner with MM logo + orange branding splash (matching Web loading screen)

**App.razor:**
- Added `<MudPopoverProvider />`, `<MudDialogProvider />`, `<MudSnackbarProvider />`
- Updated 404 page with MudBlazor components

**_Imports.razor:**
- Added `@using MudBlazor`

**Components/Layout/MainLayout.razor:**
- Full rewrite — `MudThemeProvider` with identical palette as Web (`Primary=#FF6D00`, `Secondary=#1A1A2E`, etc.)
- `MudMainContent` with full-height flex column for mobile

**Components/Pages/LoginPage.razor:**
- Rewritten with MudBlazor — `MudPaper` card with orange header (matches Web login page)
- MM logo in header, `MudTextField` for Server URL + API Key, `MudButton` with spinner
- `MudSnackbar` for connection feedback (replaces custom alert)

**Components/Pages/ScannerPage.razor:**
- Rewritten with MudBlazor — `MudAppBar` top bar with MM logo + store/device name + logout (matches Web AppBar styling)
- Scan overlay uses `MudPaper` + `MudButton` with orange primary
- Bottom cart bar uses `MudButton` (Secondary/Primary variants)
- Cart dialog via `IDialogService.ShowAsync<CartDialog>()` (replaces inline modal)
- Toasts via `ISnackbar` (replaces custom toast div)
- Camera container and scanner-status overlay preserved as-is

**wwwroot/css/app.css:**
- Stripped to camera/scanner-only styles (login, button, toast, modal CSS removed — now handled by MudBlazor theme)
- Set body font to Inter

#### Brand Identity

- **Logo:** `wwwroot/media/logo.svg` — orange (`#FF6D00`) rounded square with white "MM" text
- **Login page:** MM logo in dark square with orange header background
- **Scanner AppBar:** Small MM logo alongside store/device name
- **Loading splash:** MM logo + "StockScanTool" text (same style as Web)

#### Consistency Checklist

| Aspect | Web | PWA (Before) | PWA (After) |
|--------|-----|-------------|-------------|
| Component Library | MudBlazor 7.x | None (hand-rolled HTML) | MudBlazor 7.x |
| Primary Color | `#FF6D00` (orange) | `#1A1A2E` (navy) | `#FF6D00` (orange) |
| Font | Inter (Google Fonts) | System stack | Inter (Google Fonts) |
| Login Card | MudPaper + orange header | Plain div + white card | MudPaper + orange header (matches Web) |
| Top Bar | MudAppBar (navy + orange border) | Custom div | MudAppBar (navy + orange border, matches Web) |
| Toasts | MudSnackbar | Custom div | MudSnackbar |
| Dialogs | MudDialog | Inline modal | MudDialog (CartDialog) |
| Theme | MudThemeProvider | None | MudThemeProvider (same palette) |

---

---

### Session 5 — RBAC + User Management (Database-Backed Auth)

- **Date:** 2026-07-28
- **Model:** opencode/big-pickle
- **User Request:** "plan and implement... role based access control support for admin users - users can manage roles, users and permissions"
- **Decision:** Replaced hardcoded AdminSettings with full RBAC system: Users, Roles, Permissions tables backed by DB, permission-based `[HasPermission("module.action")]` authorization, and Web UI for management.

#### Bug Fixes

**Bug #9 (MEDIUM) — DeviceService API key lost after creation (`DeviceService.cs:38-53`):**
- After `SaveChangesAsync()`, the generated API key was no longer available on the entity because EF Core may have cleared it. The response always had a null/empty `ApiKey` field.
- **Fix:** Captured the raw API key into a local variable before hashing. The response is constructed from the local variable (raw key) instead of reading from the entity after save.
- **File:** `DeviceService.cs`

**Bug #10 (MEDIUM) — SeedData static flag (`SeedData.cs:13-20`):**
- The `static bool _isSeeded` flag was being checked inside `using (var scope = ...)` in `Program.cs`, which recreates the scope each time. But the static flag was per `SeedData` instance, not per `AppDbContext`, causing seed data to not run on subsequent app starts after a build.
- **Fix:** Removed the static flag entirely. The seed method now checks `if (!await context.Roles.AnyAsync())` at the start to determine if seeding is needed.
- **File:** `SeedData.cs`

**Bug #11 (LOW) — Store Name/Address max length mismatch (`StoreConfiguration.cs:18-19`, `StoreValidator.cs:9-10`):**
- EF configuration had `HasMaxLength(100)` for Name and `HasMaxLength(200)` for Address, but the FluentValidation validators allowed up to 200 and 500 characters respectively. A valid DTO could pass validation but fail at the database level.
- **Fix:** Aligned EF configurations with validator limits: Name → 200, Address → 500.
- **Files:** `StoreConfiguration.cs`, `ProductConfiguration.cs` (barcode: 100)

**Bug #12 (LOW) — ProductService no duplicate SKU/barcode validation (`ProductService.cs:59-65`):**
- Create and update operations did not check for duplicate SKU or barcode values, allowing duplicate entries.
- **Fix:** Added `AnyAsync` checks for duplicate SKU and barcode before create and update operations, throwing `ValidationException` when conflicts are detected.
- **File:** `ProductService.cs`

#### Changes Made

**New Domain Entities (`StockScanTool.Domain/Entities/`):**
- `User.cs` — `Username`, `PasswordHash`, `DisplayName`, `IsActive`, `UserRoles` collection
- `Role.cs` — `Name`, `Description`, `IsActive`, `UserRoles` collection, `RolePermissions` collection
- `Permission.cs` — `Code` (e.g. `"stores.read"`), `Name`, `Description`, `GroupName` (e.g. `"Stores"`)
- `UserRole.cs` — Join entity: `UserId` + `RoleId` composite PK
- `RolePermission.cs` — Join entity: `RoleId` + `PermissionId` composite PK

**New DTOs (`StockScanTool.Contracts/`):**
- `UserDtos.cs` — `UserDto`, `CreateUserRequest`, `UpdateUserRequest`, `AssignRolesRequest`
- `RoleDtos.cs` — `RoleDto`, `CreateRoleRequest`, `UpdateRoleRequest`, `AssignPermissionsRequest`
- `PermissionDtos.cs` — `PermissionDto`

**New Validators (`StockScanTool.Contracts/Validators/`):**
- `UserValidators.cs` — `CreateUserRequestValidator`, `UpdateUserRequestValidator`
- `RoleValidators.cs` — `CreateRoleRequestValidator`, `UpdateRoleRequestValidator`

**New Service Interfaces (`StockScanTool.Application/Services/`):**
- `IUserService.cs` — `GetAllAsync`, `GetByIdAsync`, `GetByUsernameAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- `IRoleService.cs` — `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- `IPermissionService.cs` — `GetAllAsync`
- Updated `IJwtTokenService.cs` — Added `GenerateUserToken(int userId, string username, string displayName, IList<string> roles, IList<string> permissions)`

**EF Core Configurations (`StockScanTool.Infrastructure/Configurations/`):**
- `UserConfiguration.cs` — Unique index on Username (max 50), PasswordHash 256, DisplayName 100
- `RoleConfiguration.cs` — Unique index on Name (max 100), Description max 500
- `UserRoleConfiguration.cs` — Composite PK (UserId, RoleId), FK cascade
- `PermissionConfiguration.cs` — Unique index on Code (max 100), Name max 200, Description max 500, GroupName max 100
- `RolePermissionConfiguration.cs` — Composite PK (RoleId, PermissionId), FK cascade

**New Service Implementations (`StockScanTool.Infrastructure/Services/`):**
- `UserService.cs` — Creates users with `PasswordHasher.Hash()`, assigns roles, validates duplicate username, throws `FluentValidation.ValidationException` on validation failure
- `RoleService.cs` — Creates roles with permission assignments, validates duplicate name
- `PermissionService.cs` — Read-only list of all permissions

**Updated Infrastructure:**
- `AuthService.cs` — Constructor now takes `IRepository<User> userRepo` alongside existing deps. `LoginUserAsync(string username, string password)` queries the `Users` table, verifies password via `PasswordHasher.Verify()`, checks `IsActive`, loads roles + permissions, and calls `IJwtTokenService.GenerateUserToken(...)`.
- `JwtTokenService.cs` — Added `GenerateUserToken` that embeds `displayName`, `role[]` (multiple), and `permission[]` (multiple) claims.
- `AppDbContext.cs` — Added `DbSet<User>`, `DbSet<Role>`, `DbSet<Permission>`, `DbSet<UserRole>`, `DbSet<RolePermission>`
- `SeedData.cs` — Seeds 26 permissions (dashboard, stores.*, products.*, devices.*, inventory.*, sales.*, users.*, roles.*, scanning.sell), 3 roles (Admin=all perms, Manager=operational perms, Viewer=read-only perms), and 1 admin user (admin/admin) with Admin role.
- `DependencyInjection.cs` — Registered `IUserService`/`UserService`, `IRoleService`/`RoleService`, `IPermissionService`/`PermissionService`, `AuthService`

**New Authorization (`StockScanTool.Api/Authorization/`):**
- `HasPermissionAttribute.cs` — Extends `AuthorizeAttribute` with policy naming: `[HasPermission("stores.read")]` → policy `"Permission_stores.read"`
- `PermissionAuthorizationHandler.cs` — `AuthorizationHandler<PermissionRequirement>` checks `context.User.HasClaim("permission", requirement.Permission)`
- `Program.cs` — Registers 26 permission policies via `AddPolicy()`, adds `PermissionAuthorizationHandler` as singleton

**Updated API:**
- `AuthController.cs` — `AdminLogin` action now delegates to `AuthService.LoginUserAsync()` (was hardcoded hash comparison)
- `Program.cs` — Removed `AdminSettings` configuration and hash generation; added `builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>()` and 26 `AddPolicy()` calls
- `appsettings.json` — Removed `AdminSettings` section entirely
- New controllers: `UsersController`, `RolesController`, `PermissionsController` with `[HasPermission]` attributes
- Existing controllers updated with `[HasPermission]` attributes:
  - `StoresController`: `stores.read`, `stores.create`, `stores.update`, `stores.delete`
  - `ProductsController`: `products.read`, `products.create`, `products.update`, `products.delete`
  - `DevicesController`: `devices.read`, `devices.create`, `devices.update`, `devices.delete`
  - `InventoryController`: `inventory.read`, `inventory.create`, `inventory.update`
  - `SalesController`: `sales.read`, `sales.create`
  - `DashboardController`: `dashboard.read`

**Updated Web UI (`StockScanTool.Web/`):**
- `ApiClient.cs` — Added `GetUsers`, `GetUser`, `CreateUser`, `UpdateUser`, `DeleteUser`, `GetRoles`, `GetRole`, `CreateRole`, `UpdateRole`, `DeleteRole`, `GetPermissions`
- `ApiRoutes.cs` — Added `Users`, `Roles`, `Permissions` route constants
- `MainLayout.razor` — Added Users/Roles nav links under "Administration" divider, dynamic `displayName` from JWT via `authGetUserInfo` JS interop
- `auth.js` — Added `decodeToken()` base64 JWT decoder, `authGetUserInfo()`, stores `displayName`/`roles`/`permissions` in localStorage on login
- `UsersPage.razor` — DataGrid with search, status chips, add/edit/delete via `UserDialog`
- `UserDialog.razor` — Role select dropdown (fetches live roles), password field on create only, username disabled on edit
- `RolesPage.razor` — DataGrid with permission chips (shows first 4 + "+N"), search, add/edit/delete via `RoleDialog`
- `RoleDialog.razor` — Permission checkboxes grouped by `GroupName` in scrollable container, save sends selected permission IDs

**Updated Tests:**
- `AuthServiceTests.cs` — Rewritten for new constructor (`IRepository<User>`), added 4 user login tests (valid credentials, not found, inactive, wrong password), 4 device tests preserved
- `UserServiceTests.cs` — New: 8 tests (GetAll, GetById found/not found, Create valid/duplicate, Update found/not found, Delete found/not found)
- `RoleServiceTests.cs` — New: 8 tests (GetAll, GetById found/not found, Create valid/duplicate, Update found/not found, Delete found/not found)
- `ApiTests.cs` — Updated `GenerateTestToken()` to emit 26 specific permission claims (was using wildcards that didn't match the exact claim check)

**EF Migration:**
- `20260728165156_AddUserRolePermission.cs` — Creates Users, Roles, Permissions, UserRoles, RolePermissions tables; adds CreatedAt/UpdatedAt/RowVersion columns to existing tables

#### Verification

- Build: 0 errors
- Tests: 84/84 pass (77 unit + 7 integration)

---

### Architecture

```
StockScanTool.Web (Blazor WASM + MudBlazor)  →  StockScanTool.Api (ASP.NET Core)
StockScanTool.ScannerPwa (Blazor WASM + MudBlazor)  →  StockScanTool.Api (ASP.NET Core)
                                                        ↓
                                                  StockScanTool.Infrastructure (EF Core)
                                                        ↓
                                                  StockScanTool.Domain (Entities)
```

### Key Decisions Made by AI

1. **Blazor WASM over Blazor Server** — Eliminated SignalR dependency, improved scalability
2. **MudBlazor over custom CSS** — Professional UI components, consistent theming
3. **localStorage over sessionStorage** — Token persists across browser sessions
4. **Direct API calls from WASM** — Removed server-side auth proxy, simpler architecture
5. **Added `[Authorize]` to Products/Stores** — Security hardening (was unprotected)
6. **Added Stores CRUD page** — Full API existed but had no UI
7. **Removed API key display from Devices page** — Was showing hashed value (security/usability bug)
8. **MudBlazor added to ScannerPwa** — Both frontends (Web admin + Scanner PWA) now share the same MudBlazor theme, component patterns, and visual identity
9. **DB-backed auth over hardcoded config** — Admin credentials now stored in Users table with PBKDF2 hashing, not in appsettings.json
10. **Permission-based over role-based** — `[HasPermission("module.action")]` attribute granularity, not just `[Authorize(Role="Admin")]`. Permissions checked via JWT claims.
11. **Multi-claim permissions** — Admin JWT tokens carry individual `permission` claims (e.g. `stores.read`, `products.create`) rather than a single role claim, allowing fine-grained authorization even within the Web UI.

### Color Palette

| Role | Color | Hex |
|------|-------|-----|
| Primary | Orange | `#FF6D00` |
| Secondary | Black | `#1A1A2E` |
| Surface | White | `#FFFFFF` |
| Success | Green | `#2E7D32` |
| Error | Red | `#D32F2F` |
| Warning | Amber | `#F9A825` |

### Default Credentials

- **Admin:** `admin` / `admin`
- **API Base URL:** `http://localhost:5168`
- **Web URL:** `https://localhost:5001`

### Port Allocation

| Service | HTTP | HTTPS |
|---------|------|-------|
| API | 5168 | 5169 |
| Web (WASM) | 5000 | 5001 |

---

## Instructions for Future AI Agents

1. **Read this file first** to understand what has been changed and why
2. **Read `Documentation/01-Architecture.md`** before making structural changes
3. **Check `Documentation/03-API-Reference.md`** before modifying API endpoints
4. **Follow the existing MudBlazor patterns** — look at existing pages for component usage
5. **Keep the `ApiResponse<T>` envelope** — all API responses are wrapped in `{ success, data, error, errors }`
6. **Auth pattern:** All protected pages check `AuthState.IsAuthenticated` in `OnInitializedAsync()` and redirect to `/login` if false
7. **API pattern:** All controllers use `[Authorize]` (except Auth and barcode lookup) + `[HasPermission("...")]` for fine-grained access
8. **Do not remove `[Authorize]` from Products or Stores controllers** — this was a security fix
9. **Both frontends use MudBlazor** — Web admin (`StockScanTool.Web`) and Scanner PWA (`StockScanTool.ScannerPwa`) share the same theme palette. Changes to one should be mirrored in the other for visual consistency.
10. **PWA-specific components:** `CartDialog.razor` (MudBlazor dialog) for cart management. Scanner camera uses `BarcodeScannerService` (JS interop via `html5-qrcode`).
11. **Update this memory file** after completing any significant changes
12. **Update `Documentation/` files** if API endpoints, entities, or configuration change
13. **Permissions are checked by exact claim match** — `HasPermission("stores.read")` checks for a JWT claim `"permission"` with value `"stores.read"`. No wildcard matching. When generating test tokens, include all individual permission claims needed.
14. **Seed data creates default admin user** — Username `admin`, password `admin`, assigned to `Admin` role (which has all 26 permissions). The old `AdminSettings` appsettings.json section has been removed.
15. **Password hashing** — Uses PBKDF2 with SHA-256 (100k iterations, 16-byte salt, 32-byte hash), not the old SHA-256-only approach.

---

### Session 6 — API Connection Fix + DB Migration Hardening

- **Date:** 2026-07-28
- **Model:** opencode/big-pickle
- **User Request:** Debug "Could not connect to the API" error and script shutdown.
- **Decision:** Fixed two root-cause issues: (1) missing `wwwroot/appsettings.json` for Blazor WASM config, (2) stale SQLite DB causing migration crash.

#### Bugs Fixed

**Bug #13 (CRITICAL) — Blazor WASM HttpClient used wrong API base URL:**
- `StockScanTool.Web/appsettings.json` (project root) was not served to the browser by the Blazor dev server. `builder.Configuration["ApiBaseUrl"]` returned `null`, causing fallback to `builder.HostEnvironment.BaseAddress` = `"http://localhost:5000/"` (the Web dev server itself). All Blazor C# API calls (dashboard, stores, products, etc.) hit the wrong server.
- Login worked because `auth.js` reads from `<meta name="api-base-url" content="http://localhost:5168">` in `index.html`, bypassing the broken config.
- **Fix:** Created `wwwroot/appsettings.json` with `{"ApiBaseUrl": "http://localhost:5168"}` — the standard location for Blazor WASM config files. Also updated root `appsettings.json` from `127.0.0.1` to `localhost` for consistency.
- **Files:** `wwwroot/appsettings.json` (new), `appsettings.json` (updated)

**Bug #14 (HIGH) — SQLite migration crash on stale database:**
- `StockScanTool.db` was previously created by `EnsureCreatedAsync()` (no migration history), so `MigrateAsync()` failed with `"table Products already exists"`. The `catch (InvalidOperationException)` block didn't catch `SqliteException`, so the process crashed.
- **Fix:** Changed catch block to `catch (Exception ex) when (ex is InvalidOperationException or SqliteException)`, added `using Microsoft.Data.Sqlite;`. Deleted stale database.
- **Files:** `Program.cs` (Api)

#### Changes Made

**`run-all.sh`:**
- Added `disown` after each `dotnet run ... &` background process launch so child processes survive the script's shell exit.

**CORS (`Program.cs` + `appsettings.json` for API):**
- Added `http://localhost:5050` to `Cors:AllowedOrigins` so the ScannerPWA can call the API.

**Documentation:**
- `Documentation/06-Configuration.md` — Added `wwwroot/appsettings.json` docs, updated CORS and config examples
- `Documentation/02-Getting-Started.md` — Updated troubleshooting with `wwwroot/appsettings.json` info

#### Verification

- All 3 services (API:5168, Web:5000, PWA:5050) start and persist after shell exit
- `wwwroot/appsettings.json` is served at `http://localhost:5000/appsettings.json`
- Login with `admin`/`admin` works, dashboard data returns successfully

**Bug #15 (HIGH) — Device API key never displayed after creation:**
- `DeviceDialog.razor` called `await Api.CreateDevice(...)` but discarded the returned `DeviceDto.ApiKey`, so the user never saw the key. The API correctly returns it.
- **Fix:** Captured the return value and shows a modal dialog with the API key after creation, with a "Copy this key now — it will not be shown again" warning.
- **File:** `DeviceDialog.razor` (Web)

---

### Session 7 — Product Image Upload

- **Date:** 2026-07-28
- **Model:** opencode/big-pickle
- **User Request:** "What about a lib that also provide upload a image?" (design question about adding product image upload)
- **Decision:** Implemented product image upload using MudFileUpload (already in the project via MudBlazor) with server-side file storage.

#### Changes Made

**Domain (`StockScanTool.Domain/Entities/Product.cs`):**
- Added `string? ImagePath` property to store the image relative URL

**Contracts (`StockScanTool.Contracts/Products/ProductDtos.cs`):**
- Added `string? ImageUrl = null` to `ProductDto` (optional, defaults to null)

**Infrastructure:**
- `Configurations/ProductConfiguration.cs:17` — Added `HasMaxLength(500)` for `ImagePath`
- `Services/EntityMapper.cs:12` — Maps `ImagePath` → `ImageUrl` in `ToDto()`
- `Services/ProductService.cs:66-75` — New `UpdateImagePathAsync(int id, string? imagePath)` method for updating the image path after upload/delete

**Application (`StockScanTool.Application/Services/IProductService.cs`):**
- Added `UpdateImagePathAsync` to the interface

**API:**
- `Controllers/ProductsController.cs:74-127` — Added two new endpoints:
  - `POST /api/v1/products/{id}/image` — Uploads a product image (multipart form, accepts JPG/PNG/GIF/WebP, max 5 MB via `[RequestSizeLimit]`). Stores in `wwwroot/uploads/products/`. Replaces old image if one exists.
  - `DELETE /api/v1/products/{id}/image` — Deletes the image file and clears the `ImagePath` field
- `Program.cs:170-174` — Added `app.UseStaticFiles()` middleware and directory creation for uploads

**Web Admin:**
- `Components/Pages/Products/ProductDialog.razor` — Added `MudFileUpload<IBrowserFile>` with file-type filter (`image/jpeg,image/png,image/gif,image/webp`), client-side base64 preview, "Remove Image" button for existing images. Image uploads happen asynchronously after product save (create/update). Tracks `_imageFile`, `_previewDataUrl`, `_imageRemoved` state.
- `Services/ApiClient.cs:95,167,255` — Added `UploadProductImage(int productId, Stream, string fileName)` using `MultipartFormDataContent`, and `DeleteProductImage(int productId)` using existing `DeleteAsync` helper

**Documentation:**
- `Documentation/03-API-Reference.md` — Added POST/DELETE image endpoints with curl examples and response shapes
- `Documentation/04-Database-Schema.md` — Added `ImagePath` field to Product table (ASCII ER diagram + field table)

#### Verification

- Build: 0 errors, pre-existing warnings only
- Images stored at `wwwroot/uploads/products/{id}.{ext}`
- Static files served via `UseStaticFiles()` at `/uploads/products/{id}.{ext}`
- Client-side preview via base64 data URL (max 5 MB file size)

---

### Session 8 — Barcode Scanner Consolidation & Cleanup

- **Date:** 2026-07-28
- **Model:** opencode/big-pickle
- **User Request:** "proceed and completly clean up, tests, documenation, memory file" (for barcode scanning improvements)
- **Decision:** Consolidated all barcode scanning into `StockScanTool.Shared`, replaced CDN-loaded `html5-qrcode` with native `BarcodeDetector` API (Chromium) + locally bundled `html5-qrcode` fallback, removed duplicated JS files, removed unused MAUI Scanner project.

#### Changes Made

**New/Consolidated Files:**

- `StockScanTool.Shared/Services/BarcodeScannerService.cs` — C# JS interop service (moved from ScannerPwa, made shared). Provides `StartScanningAsync`/`StopScanningAsync` with `OnBarcodeDetected` event.
- `StockScanTool.Shared/wwwroot/lib/html5-qrcode.min.js` — Locally bundled fallback library (375 KB, no CDN dependency).
- `StockScanTool.ScannerPwa/wwwroot/js/scanner.js` — Consolidated scanner JS with `BarcodeDetector` API as primary + `html5-qrcode` fallback. Exposes `stockScan.startContinuous()`, `stockScan.scanOnce()`, `stockScan.stop()`.
- `StockScanTool.Web/wwwroot/js/scanner.js` — Same consolidated scanner JS (identical copy).

**Updated Projects:**

- `StockScanTool.Shared.csproj` — Added `Microsoft.JSInterop` 10.0.10 package reference.
- `StockScanTool.Web.csproj` — Added `StockScanTool.Shared` project reference.
- `StockScanTool.ScannerPwa/Program.cs` — Registered `BarcodeScannerService` from Shared.
- `StockScanTool.Web/Program.cs` — No scanner changes (uses direct JS interop in ProductDialog).
- `StockScanTool.ScannerPwa/Components/Pages/ScannerPage.razor` — Uses `@inject StockScanTool.Shared.Services.BarcodeScannerService`.
- `StockScanTool.Web/Components/Pages/Products/ProductDialog.razor` — Changed JS call from `adminScanner.scanOnce` to `stockScan.scanOnce("admin-barcode-reader", _dotNetRef)`. Renamed `OnBarcodeScanned` → `OnBarcodeFound` for consistent callback name.
- `StockScanTool.ScannerPwa/wwwroot/index.html` — Loads `js/scanner.js?v=1` instead of `js/barcode-scanner.js?v=2`.
- `StockScanTool.Web/wwwroot/index.html` — Loads `js/scanner.js?v=1` instead of `js/admin-scanner.js` + `js/barcode-scanner.js`.

**Deleted Files:**

- `src/StockScanTool.ScannerPwa/wwwroot/js/barcode-scanner.js` — Replaced by consolidated `scanner.js`.
- `src/StockScanTool.ScannerPwa/Services/BarcodeScannerService.cs` — Moved to Shared.
- `src/StockScanTool.Web/wwwroot/js/barcode-scanner.js` — Replaced by consolidated `scanner.js`.
- `src/StockScanTool.Web/wwwroot/js/admin-scanner.js` — Replaced by consolidated `scanner.js`.
- `src/StockScanTool.Scanner/` (entire directory) — Unused MAUI Android app, no longer in solution.

**Documentation Updated:**

- `README.md` — Removed MAUI references, updated architecture diagram and project table.
- `Documentation/01-Architecture.md` — Updated scanner client descriptions (native BarcodeDetector + bundled fallback), removed MAUI section.
- `Documentation/02-Getting-Started.md` — Updated troubleshooting (no CDN, native API info).

**Scanner Architecture (after consolidation):**

```
stockScan.startContinuous("id", dotNetRef)
         │
    ┌────┴────┐
    │         │
    ▼         ▼
BarcodeDetector    html5-qrcode
(Chromium API)     (bundled fallback)
    │         │
    └────┬────┘
         ▼
dotNetRef.invokeMethodAsync('OnBarcodeFound', barcode)
         │
    ┌────┴────┐
    │         │
    ▼         ▼
ScannerPwa:         Web Admin:
BarcodeScannerService   ProductDialog.OnBarcodeFound
(.OnBarcodeDetected)    (sets barcode field)
```

**Key improvements:**
1. **Native BarcodeDetector API** — GPU-level, no library download for Chrome/Edge users
2. **No CDN dependency** — `html5-qrcode` bundled locally as fallback
3. **Consolidated C# service** — `BarcodeScannerService` in Shared, single implementation
4. **Consolidated JS** — Single `scanner.js` replaces 3 duplicated files
5. **Unified callback** — Both PWA and Admin use `OnBarcodeFound` JSInvokable
6. **Removed MAUI project** — Unused native Android app eliminated (saves ~5 MB, reduces build targets)`


