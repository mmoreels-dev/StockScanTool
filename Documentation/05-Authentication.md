# Authentication & Authorization

## Overview

StockScanTool uses **JWT (JSON Web Token)** Bearer authentication with two distinct token types and permission-based authorization:

| Type | Purpose | Token Source | Auth Method |
|------|---------|-------------|-------------|
| `User` | Admin dashboard access (permission-based) | Username/password login | `[HasPermission("module.action")]` |
| `Device` | Scanner operations (submit sales, look up barcodes) | API key login | `[Authorize]` + device policy |

## Authentication Flows

### Admin Login Flow

```
┌──────────┐         ┌──────────┐         ┌──────────┐
│  Browser │         │  API     │         │  Users   │
│  (Web)   │         │  Server  │         │  Table   │
└────┬─────┘         └────┬─────┘         └────┬─────┘
     │                    │                    │
     │  POST /auth/       │                    │
     │  admin-login       │                    │
     │  {username,pwd}    │                    │
     │───────────────────→│                    │
     │                    │  Query user by     │
     │                    │  username          │
     │                    │───────────────────→│
     │                    │                    │
     │                    │  User found +      │
     │                    │  active?           │
     │                    │←───────────────────│
     │                    │                    │
     │                    │  Verify password:  │
     │                    │  PBKDF2(input)     │
     │                    │  vs stored hash    │
     │                    │                    │
     │                    │  Load roles +      │
     │                    │  permissions       │
     │                    │───────────────────→│
     │                    │                    │
     │                    │  Generate JWT      │
     │                    │  (roles[],         │
     │                    │   permissions[])   │
     │                    │                    │
     │  { token: "..." }  │                    │
     │←───────────────────│                    │
     │                    │                    │
     │  Decode JWT,       │                    │
     │  store displayName,│                    │
     │  roles, permissions│                    │
     │  in localStorage   │                    │
```

**Steps:**
1. User enters username + password in the login form
2. Browser sends `POST /api/v1/Auth/admin-login`
3. API queries the `Users` table by username
4. If found and `IsActive`, verifies password via PBKDF2 (SHA-256, 100k iterations, 16-byte salt)
5. Loads the user's roles and each role's permissions
6. Generates JWT with `displayName`, `role[]` (all roles), and `permission[]` (all accumulated permissions) claims
7. Token returned to browser, stored in `localStorage`
8. Browser decodes JWT and caches `displayName`, `roles`, `permissions` in `localStorage` for UI use

### Device Login Flow

```
┌──────────┐         ┌──────────┐         ┌──────────┐
│  Scanner │         │  API     │         │  Device  │
│  (PWA)   │         │  Server  │         │  Record  │
└────┬─────┘         └────┬─────┘         └────┬─────┘
     │                    │                    │
     │  POST /auth/       │                    │
     │  device-login      │                    │
     │  { apiKey }        │                    │
     │───────────────────→│                    │
     │                    │  Hash API key      │
     │                    │  (SHA-256)         │
     │                    │                    │
     │                    │  Lookup by hash    │
     │                    │───────────────────→│
     │                    │                    │
     │                    │  Found + Active?   │
     │                    │←───────────────────│
     │                    │                    │
     │                    │  Update LastPing   │
     │                    │                    │
     │                    │  Generate JWT      │
     │                    │  (role: "Device")  │
     │                    │  (storeId: N)      │
     │                    │                    │
     │  { token, storeId  │                    │
     │    storeName }     │                    │
     │←───────────────────│                    │
```

**Steps:**
1. Scanner sends the raw API key in `POST /api/v1/Auth/device-login`
2. API hashes the key with SHA-256
3. Looks up the device by hashed key
4. If found and active, generates JWT with `role: "Device"` and `storeId` claims
5. Updates `LastPing` timestamp
6. Returns token + device/store info

## JWT Token Structure

### User Token Claims

```json
{
  "nameid": "1",
  "unique_name": "admin",
  "displayName": "Admin User",
  "role": ["Admin"],
  "permission": ["dashboard.read", "stores.read", "stores.create", ...],
  "iss": "StockScanTool",
  "aud": "StockScanToolApp",
  "exp": 1722124800,
  "iat": 1722038400
}
```

> **Note:** User tokens carry multiple `role` and `permission` claims (as JSON arrays) for fine-grained authorization via `[HasPermission("module.action")]`.

### Device Token Claims

```json
{
  "nameid": "1",
  "role": "Device",
  "storeId": "1",
  "iss": "StockScanTool",
  "aud": "StockScanToolApp",
  "exp": 1722124800,
  "iat": 1722038400
}
```

### Token Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `SecretKey` | `StockScanToolJwtDevSecretKey2024!@#$%^&*()_+` | HMAC-SHA256 signing key (min 32 chars) |
| `Issuer` | `StockScanTool` | JWT issuer claim |
| `Audience` | `StockScanToolApp` | JWT audience claim |
| `ExpirationInMinutes` | `1440` (24 hours) | Token lifetime |

The secret key can be overridden via the `JWT_SECRET_KEY` environment variable.

## Permission-Based Access Control

Access is controlled by `[HasPermission("module.action")]` attributes that check for JWT `permission` claims. User tokens include all permissions from all assigned roles.

### 26 Registered Permissions

| Module | Permissions |
|--------|-------------|
| Dashboard | `dashboard.read` |
| Stores | `stores.read`, `stores.create`, `stores.update`, `stores.delete` |
| Products | `products.read`, `products.create`, `products.update`, `products.delete` |
| Devices | `devices.read`, `devices.create`, `devices.update`, `devices.delete` |
| Inventory | `inventory.read`, `inventory.create`, `inventory.update` |
| Sales | `sales.read`, `sales.create` |
| Users | `users.read`, `users.create`, `users.update`, `users.delete` |
| Roles | `roles.read`, `roles.create`, `roles.update`, `roles.delete` |
| Scanning | `scanning.sell` |

### Endpoint Authorization Matrix

| Endpoint | Required Permission | Device | Anonymous |
|----------|:------------------:|:------:|:---------:|
| `POST /auth/admin-login` | — | | ✅ |
| `POST /auth/device-login` | — | | ✅ |
| `GET /sales/lookup/{barcode}` | — | | ✅ |
| `GET /stores` | `stores.read` | | |
| `POST /stores` | `stores.create` | | |
| `PUT /stores/{id}` | `stores.update` | | |
| `DELETE /stores/{id}` | `stores.delete` | | |
| `GET /products` | `products.read` | | |
| `POST /products` | `products.create` | | |
| `PUT /products/{id}` | `products.update` | | |
| `DELETE /products/{id}` | `products.delete` | | |
| `GET /devices` | `devices.read` | | |
| `POST /devices` | `devices.create` | | |
| `PUT /devices/{id}` | `devices.update` | | |
| `DELETE /devices/{id}` | `devices.delete` | | |
| `GET /inventory` | `inventory.read` | | |
| `PUT /inventory` | `inventory.update` | | |
| `POST /sales` | `sales.create` | ✅ | |
| `GET /sales` | `sales.read` | | |
| `GET /sales/store/{id}` | `sales.read` | | |
| `GET /dashboard` | `dashboard.read` | | |
| `GET /users` | `users.read` | | |
| `POST /users` | `users.create` | | |
| `PUT /users/{id}` | `users.update` | | |
| `DELETE /users/{id}` | `users.delete` | | |
| `GET /roles` | `roles.read` | | |
| `POST /roles` | `roles.create` | | |
| `PUT /roles/{id}` | `roles.update` | | |
| `DELETE /roles/{id}` | `roles.delete` | | |
| `GET /permissions` | `roles.read` | | |

> **Device tokens** can still submit sales and look up barcodes, but do not carry user permission claims. The `[HasPermission]` attributes do not apply to device-authenticated requests (they use `[Authorize]` with custom device policy).

### Default Roles

| Role | Permissions |
|------|-------------|
| **Admin** | All 26 permissions |
| **Manager** | `dashboard.read`, `stores.*`, `products.*`, `devices.*`, `inventory.*`, `sales.*`, `scanning.sell` |
| **Viewer** | `dashboard.read`, `stores.read`, `products.read`, `devices.read`, `inventory.read`, `sales.read` |

## Token Lifecycle

```
┌─────────────────────────────────────────────────────────────┐
│                    Token Lifecycle                          │
│                                                             │
│  ┌─────────┐    Login     ┌──────────┐   API calls  ┌────┐ │
│  │ No Token │ ──────────→ │ Has Token │ ──────────→  │ OK │ │
│  └─────────┘              └────┬─────┘              └────┘ │
│       ↑                        │                           │
│       │                        │ Token expires              │
│       │                        ▼                           │
│       │                   ┌──────────┐                     │
│       │                   │ 401 from │                     │
│       │                   │ API      │                     │
│       │                   └────┬─────┘                     │
│       │                        │                           │
│       │   Clear token,         │                           │
│       │   redirect to login    │                           │
│       ←────────────────────────┘                           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 401 Handling in the Admin Dashboard

When the API returns HTTP 401:
1. `ApiClient` detects the 401 status
2. Calls `AuthStateService.LogoutAsync()` to clear the token from `localStorage`
3. Throws `UnauthorizedAccessException`
4. The calling page catches the exception and redirects to `/login`
5. A snackbar notification informs the user: "Session expired. Please log in again."

## API Key Security

### Hashing

Device API keys are hashed using SHA-256 before storage:

```
Raw API Key → SHA-256 Hash → Stored in Database
```

The `ApiKeyHasher` service handles this:

```csharp
var hashedKey = ApiKeyHasher.Hash(rawApiKey);
```

### Key Visibility

- **At creation:** The raw API key is returned once in the response
- **After creation:** Only the hash is stored; the raw key cannot be recovered
- **In the dashboard:** The `ApiKey` field shows the hash (not the raw key)

> **Important:** Copy the API key immediately after device creation. It cannot be retrieved later.

## Rate Limiting

Authentication endpoints are rate-limited:

| Setting | Value |
|---------|-------|
| Policy name | `auth` |
| Permit limit | 10 requests |
| Window | 1 minute |
| Queue limit | 0 (reject immediately) |
| Rejection status | 429 Too Many Requests |

## Password Security

### User Passwords

- Stored as PBKDF2 hash in the `Users` table (`PasswordHash` field)
- Algorithm: `Rfc2898DeriveBytes` with SHA-256, 100,000 iterations, 16-byte salt, 32-byte hash
- Compared using constant-time comparison (`CryptographicOperations.FixedTimeEquals`)
- Format: `{base64-hash}.{base64-salt}`

### Production Recommendations

1. **Change the default admin password** — Log in with `admin`/`admin` and create a new admin user, or update `Users` table directly
2. **Use a strong JWT secret** — Set `JWT_SECRET_KEY` environment variable to a random 32+ character string
3. **Enable HTTPS** — Use valid TLS certificates
4. **Restrict CORS** — Only allow your actual domain origins
5. **Use SQL Server** — For better concurrency and security in production
