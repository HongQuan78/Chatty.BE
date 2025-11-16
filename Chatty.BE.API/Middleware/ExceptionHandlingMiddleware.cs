using System.Net;
using System.Text.Json;
using Chatty.BE.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.BE.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger
)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

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
        var statusCode = DetermineStatusCode(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception");
        }

        var problem = new ProblemDetails
        {
            Title =
                statusCode == (int)HttpStatusCode.InternalServerError
                    ? "Internal Server Error"
                    : "Request Failed",
            Detail = exception.Message,
            Status = statusCode,
            Instance = context.TraceIdentifier,
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    private static int DetermineStatusCode(Exception exception)
    {
        if (exception is AppException appException)
        {
            return (int)appException.StatusCode;
        }

        return exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            _ => (int)HttpStatusCode.InternalServerError,
        };
    }
}
