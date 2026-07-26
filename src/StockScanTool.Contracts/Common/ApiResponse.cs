namespace StockScanTool.Contracts;

public record ApiResponse<T>(bool Success, T? Data, string? Error = null, List<string>? Errors = null)
{
    public static ApiResponse<T> Ok(T data) => new(true, data);
    public static ApiResponse<T> Fail(string error) => new(false, default, error);
    public static ApiResponse<T> Fail(List<string> errors) => new(false, default, errors.FirstOrDefault(), errors);
}
