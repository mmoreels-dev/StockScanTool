using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Authorization;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class ProductsController : BaseController
{
    private readonly IProductService _service;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    public ProductsController(IProductService service, IWebHostEnvironment env)
    {
        _service = service;
        _env = env;
    }

    [HttpGet]
    [HasPermission("products.read")]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetAll()
        => Ok(ApiResponse<List<ProductDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("paged")]
    [HasPermission("products.read")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetPaged([FromQuery] PagedRequest request)
        => Ok(ApiResponse<PagedResult<ProductDto>>.Ok(await _service.GetPagedAsync(request)));

    [HttpGet("{id:int}")]
    [HasPermission("products.read")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto
            ? Ok(ApiResponse<ProductDto>.Ok(dto))
            : NotFound(ApiResponse<ProductDto>.Fail($"Product with Id={id} not found."));

    [HttpGet("lookup/{barcode}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<BarcodeLookupResponse>>> LookupBarcode(
        string barcode, [FromQuery] int storeId)
    {
        var product = await _service.GetByBarcodeAsync(barcode);
        if (product is null)
            return NotFound(ApiResponse<BarcodeLookupResponse>.Fail($"Product with barcode '{barcode}' not found."));

        var response = new BarcodeLookupResponse(
            product.Id, product.Sku, product.Name, product.Description, product.Barcode, product.Price);
        return Ok(ApiResponse<BarcodeLookupResponse>.Ok(response));
    }

    [HttpGet("sku/{sku}")]
    [HasPermission("products.read")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetBySku(string sku)
        => await _service.GetBySkuAsync(sku) is { } dto
            ? Ok(ApiResponse<ProductDto>.Ok(dto))
            : NotFound(ApiResponse<ProductDto>.Fail($"Product with SKU '{sku}' not found."));

    [HttpPost]
    [HasPermission("products.create")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create([FromBody] CreateProductRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<ProductDto>.Ok(dto));
    }

    [HttpPut("{id:int}")]
    [HasPermission("products.update")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(int id, [FromBody] UpdateProductRequest request)
        => await _service.UpdateAsync(id, request) is { } dto
            ? Ok(ApiResponse<ProductDto>.Ok(dto))
            : NotFound(ApiResponse<ProductDto>.Fail($"Product with Id={id} not found."));

    [HttpDelete("{id:int}")]
    [HasPermission("products.delete")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpPost("{id:int}/image")]
    [HasPermission("products.update")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UploadImage(int id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<ProductDto>.Fail("No file provided."));

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(ApiResponse<ProductDto>.Fail("Invalid file type. Only JPG, PNG, GIF, and WebP are allowed."));

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsDir);

        var existing = await _service.GetByIdAsync(id);
        if (existing is null)
            return NotFound(ApiResponse<ProductDto>.Fail($"Product with Id={id} not found."));

        if (existing.ImageUrl is not null)
        {
            var oldPath = Path.Combine(_env.WebRootPath, existing.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }

        var fileName = $"{id}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var imageUrl = $"/uploads/products/{fileName}";
        var dto = await _service.UpdateImagePathAsync(id, imageUrl);
        return Ok(ApiResponse<ProductDto>.Ok(dto!));
    }

    [HttpDelete("{id:int}/image")]
    [HasPermission("products.update")]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null)
            return NotFound(ApiResponse<ProductDto>.Fail($"Product with Id={id} not found."));

        if (existing.ImageUrl is not null)
        {
            var filePath = Path.Combine(_env.WebRootPath, existing.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        await _service.UpdateImagePathAsync(id, null);
        return NoContent();
    }
}
