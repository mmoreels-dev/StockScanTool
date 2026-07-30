# Development Guide

## Code Style

The project uses `.editorconfig` for consistent formatting:

| Setting | Value |
|---------|-------|
| Indentation | 4 spaces (C#, Razor) |
| Indentation | 2 spaces (JSON, XML, CSProj) |
| Line endings | LF |
| Charset | UTF-8 |
| Trim trailing whitespace | Yes (except Markdown) |
| Final newline | Yes |

### C# Conventions

- **File-scoped namespaces** (`namespace X;` not `namespace X { }`)
- **Record types** for DTOs and request/response objects
- **Expression-bodied members** where appropriate
- **Nullable reference types** enabled
- **Implicit usings** enabled
- **No comments** unless specifically requested

### Razor Conventions

- **Component-per-file** with matching `.razor` and optional `.razor.css` files
- **Parameter naming:** PascalCase for `[Parameter]` properties
- **Event callbacks:** `EventCallback` for parent-child communication
- **Cascading parameters:** Used for dialog instances (`IMudDialogInstance`)

---

## Project Architecture

```
Domain → Application → Infrastructure → Api / Web
  ↑           ↑              ↑              ↑
Entities   Interfaces    Implementations  Entry Points
```

### Adding a New Entity

1. Create entity in `src/StockScanTool.Domain/Entities/`
2. Add `DbSet<Entity>` in `AppDbContext.cs`
3. Create EF Core configuration in `Configurations/`
4. Create DTO in `src/StockScanTool.Contracts/`
5. Create repository interface in `src/StockScanTool.Application/`
6. Implement repository in `src/StockScanTool.Infrastructure/`
7. Create service interface in `src/StockScanTool.Application/`
8. Implement service in `src/StockScanTool.Infrastructure/`
9. Add controller in `src/StockScanTool.Api/`
10. Register in `DependencyInjection.cs`
11. Delete the existing `StockScanTool.db` from `src/StockScanTool.Api/` if using `EnsureCreatedAsync()` (dev mode). The API will recreate it with the new schema on next startup.

### Adding a New Permission

1. Add permission code to `SeedData.cs` permissions array + seeding logic
2. Add the permission code string to the `permissions` array in `Program.cs` (where policies are registered)
3. Add `[HasPermission("module.action")]` attribute to the relevant controller actions
4. Create a new EF migration: `dotnet ef migrations add AddXxxPermission`
5. If the permission should be assigned to existing roles, update the seed data or provide a migration script

### Adding a New API Endpoint

1. Define route constant in `ApiRoutes.cs`
2. Add DTO if needed in `Contracts/`
3. Add service method interface in `Application/Services/`
4. Implement in `Infrastructure/Services/`
5. Add controller action in `Api/Controllers/`
6. Add `[HasPermission("module.action")]` attribute for fine-grained access control (or `[Authorize]` for device endpoints)
7. Register the permission policy in `Program.cs` (add to the `permissions` array)
8. Add FluentValidation rules in `Contracts/Validators/`

---

## Testing

### Test Projects

| Project | Framework | Purpose |
|---------|-----------|---------|
| `StockScanTool.Tests.Unit` | xUnit + Moq + FluentAssertions | Unit tests for services |
| `StockScanTool.Tests.Integration` | xUnit + WebApplicationFactory | API integration tests |

### Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test tests/StockScanTool.Tests.Unit

# Run only integration tests
dotnet test tests/StockScanTool.Tests.Integration

# Run with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~ProductServiceTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~ProductServiceTests.GetAllAsync_ReturnsAllProducts"
```

### Unit Test Structure

Tests follow the **Arrange-Act-Assert** pattern:

```csharp
[Fact]
public async Task GetAllAsync_ReturnsAllProducts()
{
    // Arrange
    var products = new List<Product> { /* test data */ };
    _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

    // Act
    var result = await _service.GetAllAsync();

    // Assert
    result.Should().HaveCount(2);
    result.First().Name.Should().Be("Widget");
}
```

### Integration Test Structure

Tests use `WebApplicationFactory` with an in-memory database:

```csharp
public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real DB with in-memory
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }
}
```

### Test Coverage

| Area | Tests | Coverage |
|------|-------|----------|
| ProductService | 11 | CRUD, barcode/SKU lookup, pagination, validation |
| StoreService | 6 | CRUD basics |
| DeviceService | 8 | CRUD, unique key generation, store resolution |
| InventoryService | 7 | CRUD, upsert, stock decrement (sufficient/insufficient) |
| SaleService | 5 | Empty cart, product not found, insufficient stock, valid sale (with re-query), validation |
| DashboardService | 1 | Summary counts and revenue |
| AuthService | 8 | Device + User login (valid/invalid credentials, inactive, password verify) |
| JwtTokenService | 6 | Token generation, validation, expiry, empty key |
| UserService | 8 | CRUD, duplicate username, role assignment |
| RoleService | 8 | CRUD, duplicate name, permission assignment |
| Integration (Stores) | 3 | GET all, GET by ID, 404 |
| Integration (Products) | 3 | GET all, POST create, POST invalid |
| Integration (Dashboard) | 1 | Unauthenticated → 401 |

---

## Branch Strategy

```
main ─────────────────────────────────────────→ (production)
  │
  ├── dev ──────────────────────────────────→ (integration)
  │     │
  │     ├── feature/xxx ──────→ PR → dev
  │     ├── fix/xxx ──────────→ PR → dev
  │     └── ...
  │
  └── test ─────────────────────────────────→ (UAT)
        │
        └── release/x.x.x ──→ PR → main
```

| Branch | Purpose | Merges Into |
|--------|---------|-------------|
| `main` | Production-ready code | — |
| `dev` | Active development | `main` (via release) |
| `test` | UAT and manual testing | `main` (via release) |
| `feature/*` | New features | `dev` |
| `fix/*` | Bug fixes | `dev` |
| `release/*` | Release preparation | `main` + `dev` |

### Workflow

1. Branch from `dev` for new work
2. Create feature/fix branch: `git checkout -b feature/my-feature dev`
3. Make changes, commit, push
4. Create PR targeting `dev`
5. After review and CI pass, merge to `dev`
6. For releases, create `release/x.x.x` from `dev`
7. Test on `test` branch
8. Merge `release` to `main` and `dev`

---

## Contributing

### Before Submitting a PR

1. **Build successfully:** `dotnet build`
2. **All tests pass:** `dotnet test`
3. **No lint warnings:** Check build output for warnings
4. **Follow code style:** Match existing patterns in the codebase

### Commit Messages

Use conventional commit format:

```
type(scope): description

Examples:
feat(products): add barcode scanning to product form
fix(api): handle null reference in inventory upsert
docs(api): update endpoint documentation
refactor(services): extract common CRUD logic
test(products): add pagination test cases
```

### Pull Request Checklist

- [ ] Code compiles without errors
- [ ] All existing tests pass
- [ ] New tests added for new functionality
- [ ] API documentation updated (if endpoints changed)
- [ ] No secrets or keys committed
- [ ] Follows existing code patterns

---

## Useful Commands

```bash
# Build
dotnet build

# Run API
dotnet run --project src/StockScanTool.Api

# Run Web
dotnet run --project src/StockScanTool.Web

# Run all tests
dotnet test

# Add EF migration
dotnet ef migrations add <Name> --project src/StockScanTool.Infrastructure --startup-project src/StockScanTool.Api

# Update database
dotnet ef database update --project src/StockScanTool.Infrastructure --startup-project src/StockScanTool.Api

# Clean build artifacts
dotnet clean
```

---

## IDE Setup

### Visual Studio 2022

1. Open `StockScanTool.sln`
2. Set multiple startup projects: `StockScanTool.Api` + `StockScanTool.Web`
3. Install extensions: ReSharper, EF Core Power Tools

### VS Code

1. Open the workspace folder
2. Install extensions: C# Dev Kit, Razor (ms-dotnettools.cshtml)
3. Use `dotnet run` commands in terminal

### JetBrains Rider

1. Open `StockScanTool.sln`
2. Configure run configurations for API and Web projects
3. Built-in EF Core support via Database tool window
