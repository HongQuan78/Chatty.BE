using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Chatty.BE.API.Middleware;

public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var correlationId = EnsureCorrelationId(context);
        var watch = Stopwatch.StartNew();

        var requestBody = await ReadRequestBody(context);

        logger.LogInformation(
            "[{CorrelationId}] Incoming Request {Method} {Url} | Body: {Body}",
            correlationId,
            context.Request.Method,
            context.Request.Path,
            requestBody
        );

        var originalBodyStream = context.Response.Body;
        await using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await next(context);

            watch.Stop();

            var responseText = await ReadResponseBody(context);

            logger.LogInformation(
                "[{CorrelationId}] Response {StatusCode} in {Elapsed} ms | Body: {Body}",
                correlationId,
                context.Response.StatusCode,
                watch.ElapsedMilliseconds,
                responseText
            );
        }
        finally
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
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

    private async Task<string> ReadRequestBody(HttpContext context)
    {
        context.Request.EnableBuffering();

        if (context.Request.ContentLength == null || context.Request.ContentLength == 0)
            return string.Empty;

        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true
        );

        var body = await reader.ReadToEndAsync();

        context.Request.Body.Position = 0;
        return body;
    }

    private async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(
            context.Response.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true
        );
        var text = await reader.ReadToEndAsync();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return text;
    }
}
