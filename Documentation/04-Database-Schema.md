# Database Schema

## Entity Relationship Diagram

```
┌──────────────────┐       ┌──────────────────┐       ┌──────────────────┐
│      Store       │       │     Product      │       │  ScanningDevice  │
├──────────────────┤       ├──────────────────┤       ├──────────────────┤
│ Id (PK)          │       │ Id (PK)          │       │ Id (PK)          │
│ Name             │       │ Sku              │       │ DeviceName       │
│ Address          │       │ Name             │       │ StoreId (FK)  ───┼───┐
│ IsActive         │       │ Description      │       │ ApiKey           │   │
│ CreatedAt        │       │ Barcode          │       │ IsActive         │   │
│ UpdatedAt        │       │ Price            │       │ LastPing         │   │
│ CreatedAt        │       │ ImagePath        │       │ CreatedAt        │   │
        │                   │ CreatedAt        │       │ UpdatedAt        │   │
        │                   │ UpdatedAt        │       └──────────────────┘   │
       │                   └──────┬───────────┘       └──────────────────┘   │
       │                          │                                          │
       │    ┌─────────────────────┼──────────────────────┐                   │
       │    │                     │                      │                   │
       │    ▼                     ▼                      ▼                   │
       │ ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐           │
       │ │  Inventory   │  │   SaleItem   │  │ SaleTransaction  │           │
       │ ├──────────────┤  ├──────────────┤  ├──────────────────┤           │
       │ │ Id (PK)      │  │ Id (PK)      │  │ Id (PK)          │           │
       │ │ ProductId(FK)│  │ SaleTxnId(FK)│  │ StoreId (FK)  ───┼───────────┘
       │ │ StoreId (FK)─┼──│ ProductId(FK)│  │ ScanningDeviceId(FK) ───────┘
       │ │ QuantityOnHand│ │ Quantity     │  │ TotalAmount      │
       │ │ CreatedAt    │  │ PriceAtSale  │  │ SaleDate         │
       │ │ UpdatedAt    │  │ CreatedAt    │  │ CreatedAt        │
       │ └──────┬───────┘  │ UpdatedAt    │  │ UpdatedAt        │
       │        │          └──────┬───────┘  └──────────────────┘
       │        │                 │
       └────────┼─────────────────┘
                │
         UNIQUE INDEX on
         (ProductId, StoreId)
```

## Cardinality Summary

```
Store        1 ──── N  ScanningDevice
Store        1 ──── N  Inventory
Store        1 ──── N  SaleTransaction
Product      1 ──── N  Inventory
Product      1 ──── N  SaleItem
ScanningDevice 1 ── N  SaleTransaction
SaleTransaction 1 ─ N  SaleItem
User         M ──── M  Role          (via UserRole)
Role         M ──── M  Permission    (via RolePermission)
```

## RBAC Entities

```
┌──────────────────┐       ┌──────────────────┐       ┌──────────────────┐
│      User        │       │      Role        │       │   Permission     │
├──────────────────┤       ├──────────────────┤       ├──────────────────┤
│ Id (PK)          │       │ Id (PK)          │       │ Id (PK)          │
│ Username  (UQ)   │       │ Name       (UQ)  │       │ Code       (UQ)  │
│ PasswordHash     │       │ Description      │       │ Name             │
│ DisplayName      │       │ IsActive         │       │ Description      │
│ IsActive         │       │ CreatedAt        │       │ GroupName        │
│ CreatedAt        │       │ UpdatedAt        │       └──────────────────┘
│ UpdatedAt        │       └──────┬───────────┘              ▲
└──────┬───────────┘              │                          │
       │                         │                          │
       │     ┌───────────┐       │     ┌──────────────┐     │
       │     │ UserRole  │       │     │ RolePermission│    │
       │     ├───────────┤       │     ├──────────────┤    │
       └─────┤ UserId(FK)│       ├─────┤ RoleId(FK)   │────┘
             │ RoleId(FK)│───────┘     │ PermissionId │
             └───────────┘             │ (FK)         │
                                       └──────────────┘
```

## Entity Details

### AuditableEntity (Base Class)

All entities inherit from `AuditableEntity`:

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Id` | `int` | auto-increment | Primary key |
| `CreatedAt` | `DateTime` | `DateTime.UtcNow` | Creation timestamp |
| `UpdatedAt` | `DateTime?` | `null` | Last update timestamp |

---

### Store

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int` | PK, auto-increment | Unique identifier |
| `Name` | `string` | Required, max 200 chars | Store name |
| `Address` | `string` | Required, max 500 chars | Physical address |
| `IsActive` | `bool` | Default `true` | Whether store is active |
| `CreatedAt` | `DateTime` | Default `UtcNow` | Inherited |
| `UpdatedAt` | `DateTime?` | Nullable | Inherited |

**Collections:** `ScanningDevices`, `Inventories`, `SaleTransactions`

---

### Product

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int` | PK, auto-increment | Unique identifier |
| `Sku` | `string` | Required, max 50 chars | Stock Keeping Unit |
| `Name` | `string` | Required, max 200 chars | Product name |
| `Description` | `string` | Max 1000 chars | Product description |
| `Barcode` | `string` | Required, max 100 chars | EAN/UPC barcode |
| `Price` | `decimal` | Required, > 0 | Unit price |
| `ImagePath` | `string?` | Max 500 chars | Relative URL to uploaded product image (e.g. `/uploads/products/1.jpg`) |
| `CreatedAt` | `DateTime` | Default `UtcNow` | Inherited |
| `UpdatedAt` | `DateTime?` | Nullable | Inherited |

**Collections:** `Inventories`, `SaleItems`

---

### Inventory

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int` | PK, auto-increment | Unique identifier |
| `ProductId` | `int` | FK → Product, **unique with StoreId** | Product reference |
| `StoreId` | `int` | FK → Store, **unique with ProductId** | Store reference |
| `QuantityOnHand` | `int` | ≥ 0 | Current stock count |
| `CreatedAt` | `DateTime` | Default `UtcNow` | Inherited |
| `UpdatedAt` | `DateTime?` | Nullable | Inherited |

**Unique Index:** `(ProductId, StoreId)` — one inventory record per product per store.

---

### SaleTransaction

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int` | PK, auto-increment | Unique identifier |
| `StoreId` | `int` | FK → Store | Where the sale occurred |
| `ScanningDeviceId` | `int` | FK → ScanningDevice | Which device processed it |
| `TotalAmount` | `decimal` | ≥ 0 | Sum of all sale items |
| `SaleDate` | `DateTime` | Default `UtcNow` | When the sale occurred |
| `CreatedAt` | `DateTime` | Default `UtcNow` | Inherited |
| `UpdatedAt` | `DateTime?` | Nullable | Inherited |

**Collections:** `SaleItems`

---

### SaleItem

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int` | PK, auto-increment | Unique identifier |
| `SaleTransactionId` | `int` | FK → SaleTransaction | Parent transaction |
| `ProductId` | `int` | FK → Product | Product sold |
| `Quantity` | `int` | > 0 | Number of units |
| `PriceAtTimeOfSale` | `decimal` | ≥ 0 | Price snapshot at sale time |
| `CreatedAt` | `DateTime` | Default `UtcNow` | Inherited |
| `UpdatedAt` | `DateTime?` | Nullable | Inherited |

---

### ScanningDevice

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int` | PK, auto-increment | Unique identifier |
| `DeviceName` | `string` | Required, max 100 chars | Human-readable name |
| `StoreId` | `int` | FK → Store | Assigned store |
| `ApiKey` | `string` | Required, SHA-256 hashed | Device authentication key |
| `IsActive` | `bool` | Default `true` | Whether device is active |
| `LastPing` | `DateTime?` | Nullable | Last login timestamp |
| `CreatedAt` | `DateTime` | Default `UtcNow` | Inherited |
| `UpdatedAt` | `DateTime?` | Nullable | Inherited |

**Collections:** `SaleTransactions`

> **Security Note:** The `ApiKey` field stores the SHA-256 hash. The original key is only returned at creation time and cannot be recovered.

---

### User

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int` | PK, auto-increment | Unique identifier |
| `Username` | `string` | Required, max 50, **unique** | Login username |
| `PasswordHash` | `string` | Required, max 256 | PBKDF2 hash |
| `DisplayName` | `string` | Required, max 100 | Display name |
| `IsActive` | `bool` | Default `true` | Whether user can log in |
| `CreatedAt` | `DateTime` | Default `UtcNow` | Inherited |
| `UpdatedAt` | `DateTime?` | Nullable | Inherited |

**Collections:** `UserRoles`

> **Password Security:** Passwords are hashed with PBKDF2 (SHA-256, 100k iterations, 16-byte salt). Raw passwords are never stored.

---

### Role

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int` | PK, auto-increment | Unique identifier |
| `Name` | `string` | Required, max 100, **unique** | Role name (e.g. "Admin") |
| `Description` | `string` | Required, max 500 | Role description |
| `IsActive` | `bool` | Default `true` | Whether role is active |
| `CreatedAt` | `DateTime` | Default `UtcNow` | Inherited |
| `UpdatedAt` | `DateTime?` | Nullable | Inherited |

**Collections:** `UserRoles`, `RolePermissions`

---

### Permission

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | `int` | PK, auto-increment | Unique identifier |
| `Code` | `string` | Required, max 100, **unique** | Permission code (e.g. `"stores.read"`) |
| `Name` | `string` | Required, max 200 | Human-readable name |
| `Description` | `string` | Required, max 500 | Description |
| `GroupName` | `string` | Required, max 100 | Group for UI (e.g. "Stores") |

**Seeded permissions (26 total):** `dashboard.read`, `stores.read/create/update/delete`, `products.read/create/update/delete`, `devices.read/create/update/delete`, `inventory.read/create/update`, `sales.read/create`, `users.read/create/update/delete`, `roles.read/create/update/delete`, `scanning.sell`

---

### UserRole (Join Table)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `UserId` | `int` | PK, FK → User | User reference |
| `RoleId` | `int` | PK, FK → Role | Role reference |

**Composite PK:** `(UserId, RoleId)`

---

### RolePermission (Join Table)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `RoleId` | `int` | PK, FK → Role | Role reference |
| `PermissionId` | `int` | PK, FK → Permission | Permission reference |

**Composite PK:** `(RoleId, PermissionId)`

---

## Seed Data

On first startup, the following data is seeded:

### Stores (6)

| # | Name | Address | IsActive |
|---|------|---------|----------|
| 1 | Downtown Branch | 123 Main St, City Center | true |
| 2 | Mall Location | 456 Commerce Ave, Shopping Mall | true |
| 3 | Airport Terminal | 789 Airport Rd, Terminal 2 | true |
| 4 | Suburban Plaza | 321 Oak Lane, Suburbia | true |
| 5 | Industrial Park | 654 Factory Blvd, Industrial Zone | true |
| 6 | Waterfront Store | 987 Harbor Dr, Waterfront | true |

### Roles (3)

| # | Name | Description | Permissions |
|---|------|-------------|-------------|
| 1 | Admin | Full system access | All 26 permissions |
| 2 | Manager | Operational CRUD | stores.*, products.*, devices.*, inventory.*, sales.*, dashboard.read, scanning.sell |
| 3 | Viewer | Read-only access | stores.read, products.read, devices.read, inventory.read, sales.read, dashboard.read |

### Admin User (1)

| Username | Password | Display Name | Role |
|----------|----------|-------------|------|
| `admin` | `admin` | Admin User | Admin |

No products, devices, inventory, or sales are seeded. These must be created through the admin dashboard or API.

## Database Providers

### SQLite (Default)

```json
{
  "UseSqlite": true,
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=StockScanTool.db"
  }
}
```

- Database file created in the API project's working directory
- No external database server required
- Suitable for development and small deployments

### SQL Server

```json
{
  "UseSqlite": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=StockScanToolDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

- Requires SQL Server instance
- Suitable for production deployments
- Supports `MultipleActiveResultSets` for concurrent operations

## Migrations

The project uses EF Core Code First with migrations:

```bash
# Create a new migration
dotnet ef migrations add <MigrationName> --project src/StockScanTool.Infrastructure --startup-project src/StockScanTool.Api

# Apply migrations
dotnet ef database update --project src/StockScanTool.Infrastructure --startup-project src/StockScanTool.Api
```

> **Note:** In development, the database is auto-created via `EnsureCreatedAsync()`. Migrations are used for production deployments.
