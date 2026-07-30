# API Reference

Base URL: `http://localhost:5168`

All responses are wrapped in:
```json
{
  "success": true,
  "data": { ... },
  "error": null,
  "errors": null
}
```

## Authentication

### POST `/api/v1/Auth/admin-login`

Authenticate as admin. Returns a JWT token.

**Auth:** None (anonymous)  
**Rate Limited:** 10 requests/minute

**Request:**
```json
{
  "username": "admin",
  "password": "admin"
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs..."
  }
}
```

**Response (401):**
```json
{
  "success": false,
  "error": "Invalid credentials."
}
```

**curl:**
```bash
curl -X POST http://localhost:5168/api/v1/auth/admin-login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'
```

---

### POST `/api/v1/Auth/device-login`

Authenticate a scanning device by API key. Returns a JWT token with device info.

**Auth:** None (anonymous)  
**Rate Limited:** 10 requests/minute

**Request:**
```json
{
  "apiKey": "your-api-key-here"
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "deviceId": 1,
    "deviceName": "Scanner-01",
    "storeId": 1,
    "storeName": "Downtown Branch",
    "token": "eyJhbGciOiJIUzI1NiIs..."
  }
}
```

**curl:**
```bash
curl -X POST http://localhost:5168/api/v1/auth/device-login \
  -H "Content-Type: application/json" \
  -d '{"apiKey":"your-api-key"}'
```

---

## Stores

All store endpoints require **Admin JWT** authentication.

### GET `/api/v1/Stores`

Get all stores.

**Auth:** Admin JWT  
**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Downtown Branch",
      "address": "123 Main St, City Center",
      "isActive": true
    }
  ]
}
```

**curl:**
```bash
curl http://localhost:5168/api/v1/stores \
  -H "Authorization: Bearer <token>"
```

---

### GET `/api/v1/Stores/paged`

Get stores with pagination.

**Auth:** Admin JWT  
**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Page` | int | 1 | Page number |
| `PageSize` | int | 10 | Items per page |
| `SortBy` | string | null | Sort field |
| `Descending` | bool | false | Sort direction |

**Response (200):**
```json
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 6,
    "page": 1,
    "pageSize": 10,
    "totalPages": 1
  }
}
```

---

### GET `/api/v1/Stores/{id}`

Get a store by ID.

**Auth:** Admin JWT  
**Response (200):** Single `StoreDto`  
**Response (404):** Error with message

---

### POST `/api/v1/Stores`

Create a new store.

**Auth:** Admin JWT  
**Request:**
```json
{
  "name": "New Store",
  "address": "123 New St",
  "isActive": true
}
```

**Response (201):** Created `StoreDto`

---

### PUT `/api/v1/Stores/{id}`

Update an existing store.

**Auth:** Admin JWT  
**Request:**
```json
{
  "name": "Updated Name",
  "address": "Updated Address",
  "isActive": false
}
```

**Response (200):** Updated `StoreDto`  
**Response (404):** Error if not found

---

### DELETE `/api/v1/Stores/{id}`

Delete a store.

**Auth:** Admin JWT  
**Response (204):** No content  
**Response (404):** Error if not found

---

## Products

All product endpoints require **Admin JWT** authentication.

### GET `/api/v1/Products`

Get all products.

**Auth:** Admin JWT  
**Response (200):** Array of `ProductDto`:
```json
{
  "id": 1,
  "sku": "PROD-001",
  "name": "Widget",
  "description": "A fine widget",
  "barcode": "5901234123457",
  "price": 9.99
}
```

---

### GET `/api/v1/Products/paged`

Get products with pagination. Same query parameters as Stores/paged.

---

### GET `/api/v1/Products/{id}`

Get a product by ID.

---

### GET `/api/v1/Products/barcode/{barcode}`

Look up a product by barcode.

**Auth:** Admin JWT  
**Example:** `GET /api/v1/products/barcode/5901234123457`

---

### GET `/api/v1/Products/sku/{sku}`

Look up a product by SKU.

**Auth:** Admin JWT  
**Example:** `GET /api/v1/products/sku/PROD-001`

---

### POST `/api/v1/Products`

Create a new product.

**Auth:** Admin JWT  
**Request:**
```json
{
  "sku": "PROD-001",
  "name": "Widget",
  "description": "A fine widget",
  "barcode": "5901234123457",
  "price": 9.99
}
```

**Response (201):** Created `ProductDto`

---

### PUT `/api/v1/Products/{id}`

Update a product.

**Auth:** Admin JWT  
**Request:** Same as POST body  
**Response (200):** Updated `ProductDto`

---

### DELETE `/api/v1/Products/{id}`

Delete a product.

**Auth:** Admin JWT  
**Response (204):** No content

---

### POST `/api/v1/Products/{id}/image`

Upload or replace a product image. Deletes the previous image if one exists.

**Auth:** `products.update` permission  
**Request:** `multipart/form-data` with a `file` field containing the image.

| Field | Type | Description |
|-------|------|-------------|
| `file` | `IFormFile` | Image file (JPG, PNG, GIF, WebP, max 5 MB) |

**Response (200):** Updated `ProductDto` with `imageUrl` populated:

```json
{
  "success": true,
  "data": {
    "id": 1,
    "sku": "PROD-001",
    "name": "Widget",
    "description": "A fine widget",
    "barcode": "5901234123457",
    "price": 9.99,
    "imageUrl": "/uploads/products/1.jpg"
  }
}
```

**Response (400):** Invalid file type or no file provided  
**Response (404):** Product not found

**curl:**
```bash
curl -X POST http://localhost:5168/api/v1/products/1/image \
  -H "Authorization: Bearer <token>" \
  -F "file=@product.jpg"
```

---

### DELETE `/api/v1/Products/{id}/image`

Remove a product's image. Deletes the file from disk and clears the `imageUrl` field.

**Auth:** `products.update` permission  
**Response (204):** No content  
**Response (404):** Product not found

---

## Devices

All device endpoints require **Admin JWT** authentication.

### GET `/api/v1/Devices`

Get all devices.

**Auth:** Admin JWT  
**Response (200):** Array of `DeviceDto`:
```json
{
  "id": 1,
  "deviceName": "Scanner-01",
  "storeId": 1,
  "storeName": "Downtown Branch",
  "apiKey": "<hashed>",
  "isActive": true,
  "lastPing": "2026-07-27T10:30:00Z"
}
```

> **Note:** The `apiKey` field contains the SHA-256 hash, not the original key. The original key is only shown at creation time.

---

### GET `/api/v1/Devices/{id}`

Get a device by ID.

---

### POST `/api/v1/Devices`

Create a new device. Generates a unique API key.

**Auth:** Admin JWT  
**Request:**
```json
{
  "deviceName": "Scanner-01",
  "storeId": 1
}
```

**Response (201):** Created `DeviceDto` (包含 generated API key)

---

### PUT `/api/v1/Devices/{id}`

Update a device.

**Auth:** Admin JWT  
**Request:**
```json
{
  "deviceName": "Scanner-01",
  "storeId": 1,
  "isActive": true
}
```

---

### DELETE `/api/v1/Devices/{id}`

Delete a device.

**Auth:** Admin JWT  
**Response (204):** No content

---

## Inventory

All inventory endpoints require **Admin JWT** authentication.

### GET `/api/v1/Inventory`

Get all inventory records.

**Auth:** Admin JWT  
**Response (200):** Array of `InventoryDto`:
```json
{
  "id": 1,
  "productId": 1,
  "productName": "Widget",
  "productBarcode": "5901234123457",
  "storeId": 1,
  "storeName": "Downtown Branch",
  "quantityOnHand": 50
}
```

---

### GET `/api/v1/Inventory/store/{storeId}`

Get inventory for a specific store.

**Auth:** Admin JWT

---

### GET `/api/v1/Inventory/{id}`

Get an inventory record by ID.

---

### PUT `/api/v1/Inventory`

Upsert inventory (create or update). If a record exists for the given product+store, it updates the quantity. Otherwise, it creates a new record.

**Auth:** Admin JWT  
**Request:**
```json
{
  "productId": 1,
  "storeId": 1,
  "quantityOnHand": 50
}
```

**Response (200):** `InventoryDto`

---

## Sales

### POST `/api/v1/Sales`

Submit a sale transaction. Validates stock availability and decrements inventory.

**Auth:** Device JWT or Admin JWT  
**Request:**
```json
{
  "storeId": 1,
  "scanningDeviceId": 1,
  "items": [
    { "productId": 1, "quantity": 2 },
    { "productId": 3, "quantity": 1 }
  ]
}
```

**Response (200):** `SaleTransactionDto` with calculated `totalAmount`

**Errors:**
- 400 if cart is empty
- 400 if product not found
- 400 if insufficient stock

---

### GET `/api/v1/Sales`

Get all sale transactions.

**Auth:** Admin JWT  
**Response (200):** Array of `SaleTransactionDto`:
```json
{
  "id": 1,
  "storeId": 1,
  "storeName": "Downtown Branch",
  "scanningDeviceId": 1,
  "deviceName": "Scanner-01",
  "totalAmount": 29.97,
  "saleDate": "2026-07-27T10:30:00Z",
  "items": [
    {
      "id": 1,
      "productId": 1,
      "productName": "Widget",
      "quantity": 2,
      "priceAtTimeOfSale": 9.99
    }
  ]
}
```

---

### GET `/api/v1/Sales/store/{storeId}`

Get sales for a specific store.

**Auth:** Admin JWT  
**Query Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `from` | DateTime | Start date filter |
| `to` | DateTime | End date filter |

---

### GET `/api/v1/Sales/lookup/{barcode}`

Look up a product by barcode for quick scanner use.

**Auth:** None (anonymous)  
**Response (200):** `BarcodeLookupResponse`:
```json
{
  "id": 1,
  "sku": "PROD-001",
  "name": "Widget",
  "description": "A fine widget",
  "barcode": "5901234123457",
  "price": 9.99
}
```

---

## Dashboard

### GET `/api/v1/Dashboard`

Get aggregated dashboard summary.

**Auth:** Admin JWT  
**Response (200):**
```json
{
  "totalStores": 6,
  "totalProducts": 12,
  "totalDevices": 3,
  "totalSalesRevenue": 12450.00,
  "salesByStore": [
    {
      "storeId": 1,
      "storeName": "Downtown Branch",
      "transactionCount": 45,
      "totalRevenue": 5230.00
    }
  ],
  "stockLevels": [
    {
      "productName": "Widget",
      "barcode": "5901234123457",
      "storeId": 1,
      "storeName": "Downtown Branch",
      "quantityOnHand": 50
    }
  ]
}
```

---

## Users

All user endpoints require **`users.*` permission**.

### GET `/api/v1/Users`

Get all users with their assigned roles.

**Auth:** `users.read` permission  
**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "username": "admin",
      "displayName": "Admin User",
      "isActive": true,
      "roles": ["Admin"]
    }
  ]
}
```

**curl:**
```bash
curl http://localhost:5168/api/v1/users \
  -H "Authorization: Bearer <token>"
```

---

### GET `/api/v1/Users/{id}`

Get a user by ID.

**Auth:** `users.read` permission  
**Response (200):** Single `UserDto`  
**Response (404):** Not found

---

### POST `/api/v1/Users`

Create a new user with role assignment.

**Auth:** `users.create` permission  
**Request:**
```json
{
  "username": "jdoe",
  "password": "securepass123",
  "displayName": "John Doe",
  "roleIds": [1]
}
```

**Response (201):** Created `UserDto`

---

### PUT `/api/v1/Users/{id}`

Update a user's details and role assignment.

**Auth:** `users.update` permission  
**Request:**
```json
{
  "username": "jdoe",
  "displayName": "John Doe Updated",
  "isActive": true,
  "roleIds": [1, 2]
}
```

**Response (200):** Updated `UserDto`  
**Response (404):** Not found

---

### DELETE `/api/v1/Users/{id}`

Delete a user.

**Auth:** `users.delete` permission  
**Response (204):** No content  
**Response (404):** Not found

---

## Roles

All role endpoints require **`roles.*` permission**.

### GET `/api/v1/Roles`

Get all roles with their associated permissions.

**Auth:** `roles.read` permission  
**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "name": "Admin",
      "description": "Full system access",
      "isActive": true,
      "permissions": ["dashboard.read", "stores.read", "stores.create", ...]
    }
  ]
}
```

**curl:**
```bash
curl http://localhost:5168/api/v1/roles \
  -H "Authorization: Bearer <token>"
```

---

### GET `/api/v1/Roles/{id}`

Get a role by ID.

**Auth:** `roles.read` permission  
**Response (200):** Single `RoleDto`  
**Response (404):** Not found

---

### POST `/api/v1/Roles`

Create a new role with permission assignments.

**Auth:** `roles.create` permission  
**Request:**
```json
{
  "name": "Manager",
  "description": "Operational access",
  "permissionIds": [2, 3, 4, 7, 8, 9, 12, 13, 14, 17, 18]
}
```

**Response (201):** Created `RoleDto`

---

### PUT `/api/v1/Roles/{id}`

Update a role's details and permission assignments.

**Auth:** `roles.update` permission  
**Request:**
```json
{
  "name": "Manager",
  "description": "Updated description",
  "isActive": true,
  "permissionIds": [2, 3, 4, 7, 8, 9]
}
```

**Response (200):** Updated `RoleDto`  
**Response (404):** Not found

---

### DELETE `/api/v1/Roles/{id}`

Delete a role.

**Auth:** `roles.delete` permission  
**Response (204):** No content  
**Response (404):** Not found

---

## Permissions

### GET `/api/v1/Permissions`

Get all available permissions (read-only).

**Auth:** `roles.read` permission  
**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "code": "dashboard.read",
      "name": "View Dashboard",
      "description": "Access the dashboard page and summary data",
      "groupName": "Dashboard"
    },
    {
      "id": 2,
      "code": "stores.read",
      "name": "View Stores",
      "description": "View store list and details",
      "groupName": "Stores"
    }
  ]
}
```

**curl:**
```bash
curl http://localhost:5168/api/v1/permissions \
  -H "Authorization: Bearer <token>"
```

---

## Common Headers

All authenticated requests require:
```
Authorization: Bearer <jwt-token>
Content-Type: application/json
```

## Error Responses

| HTTP Status | Meaning |
|-------------|---------|
| 400 | Validation error or bad request |
| 401 | Unauthorized (missing or invalid token) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Resource not found |
| 409 | Conflict (e.g., duplicate) |
| 429 | Rate limit exceeded |
| 500 | Internal server error |
