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

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
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

    [HttpGet("paged")]
    [HasPermission("sales.read")]
    public async Task<ActionResult<ApiResponse<PagedResult<SaleTransactionDto>>>> GetPaged([FromQuery] PagedRequest request)
        => Ok(ApiResponse<PagedResult<SaleTransactionDto>>.Ok(await _saleService.GetPagedAsync(request)));

    [HttpGet("store/{storeId:int}")]
    [HasPermission("sales.read")]
    public async Task<ActionResult<ApiResponse<List<SaleTransactionDto>>>> GetByStore(
        int storeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(ApiResponse<List<SaleTransactionDto>>.Ok(await _saleService.GetByStoreAsync(storeId, from, to)));
}
