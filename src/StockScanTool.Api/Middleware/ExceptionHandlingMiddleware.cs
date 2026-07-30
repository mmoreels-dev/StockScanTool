using System.Net;
using System.Text.Json;
using FluentValidation;
using StockScanTool.Contracts;

namespace StockScanTool.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation failed.",
                validationEx.Errors.Select(e => e.ErrorMessage).ToList()
            ),
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                argEx.Message,
                (List<string>?)null
            ),
            KeyNotFoundException keyEx => (
                HttpStatusCode.NotFound,
                keyEx.Message,
                (List<string>?)null
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized access.",
                (List<string>?)null
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                (List<string>?)null
            )
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object?>(false, null, message, errors);
        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
