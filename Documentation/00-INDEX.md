# StockScanTool Documentation

Welcome to the StockScanTool developer documentation.

## Documentation Index

| # | Document | Description |
|---|----------|-------------|
| 01 | [Architecture Overview](01-Architecture.md) | Clean Architecture layers, project relationships, data flow diagrams |
| 02 | [Getting Started](02-Getting-Started.md) | Prerequisites, setup, running the application, first-time configuration |
| 03 | [API Reference](03-API-Reference.md) | All 32 REST endpoints with request/response examples |
| 04 | [Database Schema](04-Database-Schema.md) | Entity relationships, ERD diagram, seed data, constraints |
| 05 | [Authentication](05-Authentication.md) | JWT auth flows, token structure, role-based access |
| 06 | [Configuration](06-Configuration.md) | All configuration options for API and Web projects |
| 07 | [Development Guide](07-Development-Guide.md) | Code style, testing, contributing, branch strategy |

## Quick Links

- **Solution file:** `StockScanTool.sln`
- **API entry point:** `src/StockScanTool.Api/Program.cs`
- **Web entry point:** `src/StockScanTool.Web/Program.cs`
- **Domain entities:** `src/StockScanTool.Domain/Entities/`
- **DTOs & validators:** `src/StockScanTool.Contracts/`
- **Tests:** `tests/StockScanTool.Tests.Unit/` and `tests/StockScanTool.Tests.Integration/`

## Target Framework

All projects target **.NET 10.0** (except the MAUI Scanner which targets `net8.0-android`).
