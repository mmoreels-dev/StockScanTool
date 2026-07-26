using Microsoft.AspNetCore.Mvc;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

public class ProductsController : BaseController
{
    private readonly IProductService _service;

    public ProductsController(IProductService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetAll()
        => Ok(ApiResponse<List<ProductDto>>.Ok(await _service.GetAllAsync()));

    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetPaged([FromQuery] PagedRequest request)
        => Ok(ApiResponse<PagedResult<ProductDto>>.Ok(await _service.GetPagedAsync(request)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto
            ? Ok(ApiResponse<ProductDto>.Ok(dto))
            : NotFound(ApiResponse<ProductDto>.Fail($"Product with Id={id} not found."));

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetByBarcode(string barcode)
        => await _service.GetByBarcodeAsync(barcode) is { } dto
            ? Ok(ApiResponse<ProductDto>.Ok(dto))
            : NotFound(ApiResponse<ProductDto>.Fail($"Product with barcode '{barcode}' not found."));

    [HttpGet("sku/{sku}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetBySku(string sku)
        => await _service.GetBySkuAsync(sku) is { } dto
            ? Ok(ApiResponse<ProductDto>.Ok(dto))
            : NotFound(ApiResponse<ProductDto>.Fail($"Product with SKU '{sku}' not found."));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create([FromBody] CreateProductRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<ProductDto>.Ok(dto));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(int id, [FromBody] UpdateProductRequest request)
        => await _service.UpdateAsync(id, request) is { } dto
            ? Ok(ApiResponse<ProductDto>.Ok(dto))
            : NotFound(ApiResponse<ProductDto>.Fail($"Product with Id={id} not found."));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
