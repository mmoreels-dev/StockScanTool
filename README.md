# StockScanTool

A multi-store retail inventory and point-of-sale management system built with .NET 10, featuring barcode scanning, real-time stock tracking, and a modern admin dashboard.

## Architecture

```
                        Client Applications
  ┌────────────────────┐  ┌────────────────────┐
  │  Admin Web         │  │  Scanner PWA       │
  │  (Blazor WASM +    │  │  (Blazor WASM      │
  │  MudBlazor)        │  │  PWA + Camera)     │
  └────────┬───────────┘  └────────┬───────────┘
           │                       │
           └──────────┬────────────┘
                      │  HTTP + JWT Bearer
               ┌──────┴───────┐
               │  REST API    │
               │  (ASP.NET    │
               │  Core)       │
               └──────┬───────┘
                      │
               ┌──────┴───────┐
               │  EF Core     │
               │  SQLite/SQL  │
               └──────────────┘
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core Web API (.NET 10) |
| Admin Dashboard | Blazor WebAssembly + MudBlazor |
| Scanner Client | Blazor WebAssembly PWA |
| Barcode Scanning | Native BarcodeDetector API (Chromium) + html5-qrcode fallback |
| ORM | Entity Framework Core 10 |
| Database | SQLite (default) / SQL Server |
| Auth | JWT Bearer tokens |
| Validation | FluentValidation 11 |
| UI Framework | MudBlazor 7 |

## Quick Start

```bash
# 1. Build the solution
dotnet build

# 2. Start the API (terminal 1)
dotnet run --project src/StockScanTool.Api

# 3. Start the admin dashboard (terminal 2)
dotnet run --project src/StockScanTool.Web
```

**Login:** `admin` / `admin` at `https://localhost:5001` (seeded with Admin role — all permissions)

> **Production note:** The API listens on plain HTTP `:5168` in development. In production, front it with TLS termination (HTTPS on `:5169` only activates when `certs/cert.pfx` is present).

## Projects

| Project | Purpose |
|---------|---------|
| `StockScanTool.Domain` | Domain entities (zero dependencies) |
| `StockScanTool.Contracts` | DTOs, validators, API routes |
| `StockScanTool.Application` | Service & repository interfaces |
| `StockScanTool.Infrastructure` | EF Core, services, repositories |
| `StockScanTool.Api` | REST API with controllers |
| `StockScanTool.Web` | Admin dashboard (Blazor WASM) |
| `StockScanTool.ScannerPwa` | Barcode scanner PWA |
| `StockScanTool.Shared` | Shared client services (auth, lookup, scanning) |

## Documentation

Comprehensive documentation is in the [`Documentation/`](Documentation/) folder:

- [Documentation Index](Documentation/00-INDEX.md)
- [Architecture Overview](Documentation/01-Architecture.md)
- [Getting Started](Documentation/02-Getting-Started.md)
- [API Reference](Documentation/03-API-Reference.md)
- [Database Schema](Documentation/04-Database-Schema.md)
- [Authentication](Documentation/05-Authentication.md)
- [Configuration](Documentation/06-Configuration.md)
- [Development Guide](Documentation/07-Development-Guide.md)

## Branches

| Branch | Purpose |
|--------|---------|
| `main` | Production-ready code |
| `dev` | Active development — branch from here |
| `test` | UAT and manual functional testing |

## License

MIT
