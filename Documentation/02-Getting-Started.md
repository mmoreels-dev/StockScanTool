# Getting Started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- A code editor (Visual Studio 2022, VS Code, or Rider)
- Git

## Project Structure

```
StockScanTool/
├── StockScanTool.sln          ← Open this in your IDE
├── src/
│   ├── StockScanTool.Api/     ← REST API backend
│   ├── StockScanTool.Web/     ← Admin dashboard (Blazor WASM)
│   ├── StockScanTool.ScannerPwa/ ← Barcode scanner PWA
│   └── ...                    ← Other projects
├── tests/
│   ├── StockScanTool.Tests.Unit/
│   └── StockScanTool.Tests.Integration/
├── certs/                     ← TLS certificates
└── Documentation/             ← This documentation
```

## First-Time Setup

### 1. Clone and Build

```bash
git clone <repository-url>
cd StockScanTool
dotnet restore
dotnet build
```

### 2. Database

The application uses **SQLite by default**. The database file (`StockScanTool.db`) is created automatically on first API startup.

To switch to SQL Server, edit `src/StockScanTool.Api/appsettings.json`:

```json
{
  "UseSqlite": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=StockScanToolDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 3. Seed Data

On first startup, the API seeds 6 sample stores:

| Store | Address |
|-------|---------|
| Downtown Branch | 123 Main St, City Center |
| Mall Location | 456 Commerce Ave, Shopping Mall |
| Airport Terminal | 789 Airport Rd, Terminal 2 |
| Suburban Plaza | 321 Oak Lane, Suburbia |
| Industrial Park | 654 Factory Blvd, Industrial Zone |
| Waterfront Store | 987 Harbor Dr, Waterfront |

## Running the Application

### Start the API

```bash
dotnet run --project src/StockScanTool.Api
```

The API starts on:
- **HTTP:** `http://localhost:5168`
- **HTTPS:** `https://localhost:5169` (if TLS certificate exists)
- **Swagger:** `http://localhost:5168/swagger` (Development mode)

### Start the Admin Dashboard

```bash
dotnet run --project src/StockScanTool.Web
```

The dashboard starts on:
- **HTTPS:** `https://localhost:5001`
- **HTTP:** `http://localhost:5000`

### Start the Scanner PWA (optional)

```bash
dotnet run --project src/StockScanTool.ScannerPwa
```

## First Login

1. Open the admin dashboard in your browser
2. Log in with:
   - **Username:** `admin`
   - **Password:** `admin`
3. You'll see the Dashboard with the 6 seeded stores

## Creating Products

1. Navigate to **Products** in the sidebar
2. Click **Add Product**
3. Fill in SKU, Name, Barcode, and Price
4. Optionally use the **Scan** button to scan a barcode with your camera
5. Click **Create**

## Registering a Scanner Device

1. Navigate to **Devices** in the sidebar
2. Click **Add Device**
3. Enter a device name and select a store
4. Click **Create**
5. The device API key is shown on creation — copy it immediately (it cannot be retrieved later)

## Setting Inventory Levels

1. Navigate to **Inventory** in the sidebar
2. Click **Set Stock Level**
3. Select a store and product
4. Enter the quantity on hand
5. Click **Save**

## TLS Certificates

The `certs/` directory contains self-signed certificates for development:

| File | Purpose |
|------|---------|
| `cert.pfx` | PKCS#12 certificate (used by Kestrel) |
| `cert.pem` | Public certificate |
| `key.pem` | Private key |

The PFX password is `stock123` (configured in `Program.cs`).

For production, replace these with real certificates from a trusted CA.

## Troubleshooting

### "Could not connect to the API"

- Ensure the API is running on `http://localhost:5168`
- Check that CORS origins in `Api/appsettings.json` include the Web app's URL
- Ensure `src/StockScanTool.Web/wwwroot/appsettings.json` exists with `"ApiBaseUrl": "http://localhost:5168"` — the root-level `appsettings.json` is **not** served to the browser by the Blazor WASM dev server
- If you see this error **after** login, the Blazor C# `HttpClient` is likely using the wrong base URL (falling back to the Web dev server's own URL)

### Database errors

- Delete `StockScanTool.db` and restart the API to recreate from scratch
- Ensure the SQLite file is not locked by another process

### Barcode scanner not working

- Ensure your browser has camera permissions enabled
- On Chrome/Edge, the native `BarcodeDetector` API is used (fastest, no library download). Firefox and Safari fall back to the bundled `html5-qrcode` library.
- Camera access requires HTTPS or localhost
- The barcode library is bundled locally (no CDN dependency)

### Port conflicts

- Check `Properties/launchSettings.json` in each project to modify ports
- Update `ApiBaseUrl` in `Web/appsettings.json` if the API port changes
- Update `Cors:AllowedOrigins` in `Api/appsettings.json` if the Web port changes
