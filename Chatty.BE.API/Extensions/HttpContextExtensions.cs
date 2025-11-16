namespace Chatty.BE.API.Extensions;

public static class HttpContextExtensions
{
    public static string GetClientIp(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
