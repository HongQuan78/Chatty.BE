using Chatty.BE.Application.Interfaces.Services;

namespace Chatty.BE.Application.Common.Extensions;

public static class DateTimeProviderExtensions
{
    public static int SecondsUntil(this IDateTimeProvider dateTimeProvider, DateTime targetUtc)
    {
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        var seconds = (int)(targetUtc - dateTimeProvider.UtcNow).TotalSeconds;
        return seconds > 0 ? seconds : 0;
    }
}
