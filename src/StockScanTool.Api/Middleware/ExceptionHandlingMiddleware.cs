using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace StockScanTool.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

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
        context.Response.ContentType = "application/problem+json";

        var (statusCode, detail) = exception switch
        {
            ValidationException validationEx => (HttpStatusCode.BadRequest, string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))),
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            KeyNotFoundException keyEx => (HttpStatusCode.NotFound, keyEx.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access."),
            InvalidOperationException opEx => (HttpStatusCode.InternalServerError, opEx.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = statusCode.ToString(),
            Detail = detail,
            Type = $"https://httpstatuses.com/{(int)statusCode}"
        };

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
