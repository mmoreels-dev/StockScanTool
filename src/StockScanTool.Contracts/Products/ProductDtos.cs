namespace StockScanTool.Contracts;

public record ProductDto(int Id, string Sku, string Name, string Description, string Barcode, decimal Price, string? ImageUrl = null);
public record CreateProductRequest(string Sku, string Name, string Description, string Barcode, decimal Price);
public record UpdateProductRequest(string Sku, string Name, string Description, string Barcode, decimal Price);
