namespace StockScanTool.Contracts;

public static class ApiRoutes
{
    public const string Base = "api/v1";

    public static class Stores
    {
        public const string Base = $"{ApiRoutes.Base}/stores";
        public const string GetById = $"{Base}/{{id:int}}";
    }

    public static class Products
    {
        public const string Base = $"{ApiRoutes.Base}/products";
        public const string GetById = $"{Base}/{{id:int}}";
        public const string ByBarcode = $"{Base}/barcode/{{barcode}}";
        public const string BySku = $"{Base}/sku/{{sku}}";
    }

    public static class Devices
    {
        public const string Base = $"{ApiRoutes.Base}/devices";
        public const string GetById = $"{Base}/{{id:int}}";
    }

    public static class Inventory
    {
        public const string Base = $"{ApiRoutes.Base}/inventory";
        public const string GetById = $"{Base}/{{id:int}}";
        public const string ByStore = $"{Base}/store/{{storeId:int}}";
    }

    public static class Sales
    {
        public const string Base = $"{ApiRoutes.Base}/sales";
        public const string ByStore = $"{Base}/store/{{storeId:int}}";
        public const string LookupBarcode = $"{Base}/lookup/{{barcode}}";
    }

    public static class Dashboard
    {
        public const string Base = $"{ApiRoutes.Base}/dashboard";
    }

    public static class Auth
    {
        public const string DeviceLogin = $"{ApiRoutes.Base}/auth/device-login";
        public const string AdminLogin = $"{ApiRoutes.Base}/auth/admin-login";
    }
}
