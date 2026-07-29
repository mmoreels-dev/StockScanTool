namespace StockScanTool.Application.Results;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? Error { get; }
    public List<string> Errors { get; }

    private Result(T data)
    {
        IsSuccess = true;
        Data = data;
        Errors = [];
    }

    private Result(string error, List<string>? errors = null)
    {
        IsSuccess = false;
        Error = error;
        Errors = errors ?? [error];
    }

    public static Result<T> Ok(T data) => new(data);
    public static Result<T> Fail(string error) => new(error);
    public static Result<T> Fail(List<string> errors) => new(errors.Count > 0 ? errors.First() : "An error occurred.", errors);
}
