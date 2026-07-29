using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockScanTool.Api.Authorization;
using StockScanTool.Application.Services;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Controllers;

[Authorize]
public class SalesController : BaseController
{
    private readonly ISaleService _saleService;
    private readonly IProductService _productService;

    public SalesController(ISaleService saleService, IProductService productService)
    {
        _saleService = saleService;
        _productService = productService;
    }

    [HttpPost]
    [HasPermission("sales.create")]
    public async Task<ActionResult<ApiResponse<SaleTransactionDto>>> SubmitSale([FromBody] SubmitSaleRequest request)
    {
        var result = await _saleService.SubmitSaleAsync(request);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<SaleTransactionDto>.Fail(result.Errors));
        return Ok(ApiResponse<SaleTransactionDto>.Ok(result.Data!));
    }

    [HttpGet]
    [HasPermission("sales.read")]
    public async Task<ActionResult<ApiResponse<List<SaleTransactionDto>>>> GetAll()
        => Ok(ApiResponse<List<SaleTransactionDto>>.Ok(await _saleService.GetAllAsync()));

    [HttpGet("store/{storeId:int}")]
    [HasPermission("sales.read")]
    public async Task<ActionResult<ApiResponse<List<SaleTransactionDto>>>> GetByStore(
        int storeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(ApiResponse<List<SaleTransactionDto>>.Ok(await _saleService.GetByStoreAsync(storeId, from, to)));

    [HttpGet("lookup/{barcode}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<BarcodeLookupResponse>>> LookupBarcode(
        string barcode, [FromQuery] int storeId)
    {
        var product = await _productService.GetByBarcodeAsync(barcode);
        if (product is null)
            return NotFound(ApiResponse<BarcodeLookupResponse>.Fail($"Product with barcode '{barcode}' not found."));

        var response = new BarcodeLookupResponse(
            product.Id, product.Sku, product.Name, product.Description, product.Barcode, product.Price);
        return Ok(ApiResponse<BarcodeLookupResponse>.Ok(response));
    }
}
