using Microsoft.AspNetCore.Mvc;
using StockScanTool.Contracts;
using StockScanTool.Api.Services;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
        => await _service.GetByIdAsync(id) is { } dto ? Ok(dto) : NotFound();

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> GetByBarcode(string barcode)
        => await _service.GetByBarcodeAsync(barcode) is { } dto ? Ok(dto) : NotFound();

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request)
    {
        var dto = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductRequest request)
        => await _service.UpdateAsync(id, request) is { } dto ? Ok(dto) : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
