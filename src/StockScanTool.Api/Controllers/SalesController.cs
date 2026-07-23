using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Contracts;
using StockScanTool.Api.Services;

namespace StockScanTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _service;

    public SalesController(ISaleService service) => _service = service;

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<SaleTransactionDto>> SubmitSale([FromBody] SubmitSaleRequest request)
    {
        var result = await _service.SubmitSaleAsync(request);
        if (result is null)
            return BadRequest("Sale failed: invalid products, insufficient stock, or empty cart.");
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<List<SaleTransactionDto>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("store/{storeId:int}")]
    public async Task<ActionResult<List<SaleTransactionDto>>> GetByStore(
        int storeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await _service.GetSalesByStoreAsync(storeId, from, to));

    [HttpGet("lookup/{barcode}")]
    [AllowAnonymous]
    public async Task<ActionResult<BarcodeLookupResponse>> LookupBarcode(
        string barcode, [FromQuery] int storeId)
        => await _service.LookupBarcodeAsync(barcode, storeId) is { } dto ? Ok(dto) : NotFound("Product not found for this barcode.");
}
