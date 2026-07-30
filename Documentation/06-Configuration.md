# Configuration Reference

## API Configuration (`src/StockScanTool.Api/appsettings.json`)

### Database

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `UseSqlite` | `bool` | `true` | Toggle between SQLite and SQL Server |
| `ConnectionStrings:SqliteConnection` | `string` | `Data Source=StockScanTool.db` | SQLite connection string |
| `ConnectionStrings:DefaultConnection` | `string` | `Server=localhost;Database=StockScanToolDb;...` | SQL Server connection string |

### Admin Credentials

Admin credentials are now stored in the `Users` database table (seeded on first startup). The `AdminSettings` section in `appsettings.json` has been removed.

**Default admin user:** `admin` / `admin` (PBKDF2 hashed, stored in database)

### JWT Authentication

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `JwtSettings:SecretKey` | `string` | `StockScanToolJwtDevSecretKey2024!@#$%^&*()_+` | HMAC-SHA256 signing key (min 32 chars) |
| `JwtSettings:Issuer` | `string` | `StockScanTool` | JWT issuer claim |
| `JwtSettings:Audience` | `string` | `StockScanToolApp` | JWT audience claim |
| `JwtSettings:ExpirationInMinutes` | `int` | `1440` (24h) | Token lifetime |

**Environment Variable Override:**
```bash
export JWT_SECRET_KEY="your-very-long-random-secret-key-here"
```

### CORS

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Cors:AllowedOrigins` | `string[]` | `["https://localhost:5443", "http://localhost:5000", "http://localhost:5050"]` | Allowed origins for cross-origin requests |

### Rate Limiting

| Setting | Value | Location |
|---------|-------|----------|
| Rejection status | 429 Too Many Requests | `Program.cs` |
| Auth policy: permit limit | 10 requests | `Program.cs` |
| Auth policy: window | 1 minute | `Program.cs` |
| Auth policy: queue limit | 0 | `Program.cs` |

### HTTPS / TLS

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `HttpsCertPassword` | `string` | `stock123` (fallback in code) | Password for the PFX certificate |

**Kestrel Listening Ports:**

| Port | Protocol | Description |
|------|----------|-------------|
| 5168 | HTTP | API HTTP endpoint |
| 5169 | HTTPS | API HTTPS endpoint (requires `certs/cert.pfx`) |

### Logging

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Logging:LogLevel:Default` | `string` | `Information` | Default log level |
| `Logging:LogLevel:Microsoft.AspNetCore` | `string` | `Warning` | ASP.NET Core log level |

---

## Web Configuration

### Served to browser (`wwwroot/appsettings.json`)

Blazor WebAssembly loads configuration from `wwwroot/appsettings.json` at runtime. The project-root `appsettings.json` is **not** served by the dev server.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `ApiBaseUrl` | `string` | `http://localhost:5168` | Base URL of the API server |

> **Note:** The Blazor WASM app runs in the browser and makes HTTP requests to this URL. Ensure CORS is configured on the API to accept requests from the Web app's origin.
>
> **If this file is missing**, `builder.Configuration["ApiBaseUrl"]` returns `null` and falls back to `builder.HostEnvironment.BaseAddress` (the Web dev server's own URL), causing all C# API calls to fail. The JavaScript login still works because `auth.js` reads from the `<meta name="api-base-url">` tag in `index.html`.

### Project root (`appsettings.json`)

Used by .NET tooling (e.g., launch profiles). Not served to the browser. Must be mirrored in `wwwroot/appsettings.json` for runtime config.

---

## Web Launch Settings (`src/StockScanTool.Web/Properties/launchSettings.json`)

| Profile | HTTP Port | HTTPS Port | Description |
|---------|-----------|------------|-------------|
| `StockScanTool.Web` | 5000 | 5001 | Default development profile |

---

## Environment Variables

| Variable | Overrides | Description |
|----------|-----------|-------------|
| `JWT_SECRET_KEY` | `JwtSettings:SecretKey` | JWT signing secret |
| `ASPNETCORE_ENVIRONMENT` | — | Development/Production environment |
| `ASPNETCORE_URLS` | — | Kestrel listening URLs |

---

## Certificate Files

| File | Path | Purpose |
|------|------|---------|
| `cert.pfx` | `certs/cert.pfx` | PKCS#12 certificate for Kestrel HTTPS |
| `cert.pem` | `certs/cert.pem` | Public certificate (PEM format) |
| `key.pem` | `certs/key.pem` | Private key (PEM format) |

**Development certificates** are included in the `certs/` directory with password `stock123`.

For production:
1. Obtain a certificate from a trusted CA (e.g., Let's Encrypt)
2. Replace `cert.pfx` in the `certs/` directory
3. Update the password in `Program.cs` or set `HttpsCertPassword` in configuration

---

## Configuration Hierarchy

ASP.NET Core configuration follows this precedence (highest wins):

```
1. Environment variables
2. appsettings.{Environment}.json
3. appsettings.json
4. Command-line arguments
5. Default values in code
```

**Example:** Setting `JWT_SECRET_KEY` as an environment variable overrides `JwtSettings:SecretKey` in `appsettings.json`.

---

## Full Configuration File

### API `appsettings.json`

```json
{
  "UseSqlite": true,
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=StockScanTool.db",
    "DefaultConnection": "Server=localhost;Database=StockScanToolDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "SecretKey": "StockScanToolJwtDevSecretKey2024!@#$%^&*()_+",
    "Issuer": "StockScanTool",
    "Audience": "StockScanToolApp",
    "ExpirationInMinutes": 1440
  },
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:5443",
      "http://localhost:5000",
      "https://localhost:5001"
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Web project-root `appsettings.json`

```json
{
  "ApiBaseUrl": "http://localhost:5168",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

> ⚠️ This file must be **mirrored** as `wwwroot/appsettings.json` for the Blazor WASM app to read it at runtime.

### Web `wwwroot/appsettings.json`

```json
{
  "ApiBaseUrl": "http://localhost:5168"
}
```
