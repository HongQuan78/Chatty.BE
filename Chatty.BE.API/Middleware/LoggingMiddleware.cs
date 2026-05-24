using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Chatty.BE.API.Middleware;

public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var correlationId = EnsureCorrelationId(context);
        var watch = Stopwatch.StartNew();

        logger.LogInformation(
            "[{CorrelationId}] Incoming Request {Method} {Path}",
            correlationId,
            context.Request.Method,
            context.Request.Path
        );

        try
        {
            await next(context);
        }
        finally
        {
            watch.Stop();

            logger.LogInformation(
                "[{CorrelationId}] Response {StatusCode} in {Elapsed} ms",
                correlationId,
                context.Response.StatusCode,
                watch.ElapsedMilliseconds
            );
        }
    }

    private static string EnsureCorrelationId(HttpContext context)
    {
        const string header = "X-Correlation-ID";

        if (!context.Request.Headers.TryGetValue(header, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[header] = correlationId;
        }

        context.Response.Headers[header] = correlationId;
        return correlationId!;
    }
}
