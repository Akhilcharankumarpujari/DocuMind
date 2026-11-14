using System.Net;
using System.Text.Json;
using DocuMind.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.WebApi.Middleware;

/// <summary>
/// Global exception handling middleware converting uncaught domain and system exceptions
/// into standardized RFC 7807 ProblemDetails JSON responses.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        var (statusCode, title, logLevel) = exception switch
        {
            EntityNotFoundException => (HttpStatusCode.NotFound, "Resource Not Found", LogLevel.Warning),
            DomainException => (HttpStatusCode.BadRequest, "Domain Rule Violation", LogLevel.Warning),
            ArgumentException or InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid Request", LogLevel.Warning),
            _ => (HttpStatusCode.InternalServerError, "An unexpected server error occurred.", LogLevel.Error)
        };

        if (logLevel == LogLevel.Error)
        {
            _logger.LogError(exception, "Unhandled exception occurred during request {Path}. TraceId: {TraceId}", context.Request.Path, traceId);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception {ExceptionType} during request {Path}. TraceId: {TraceId}", exception.GetType().Name, context.Request.Path, traceId);
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails);
    }
}
