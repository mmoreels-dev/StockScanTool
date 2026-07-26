namespace StockScanTool.Contracts;

public record PagedRequest(int Page = 1, int PageSize = 20, string? SortBy = null, bool Descending = false);
