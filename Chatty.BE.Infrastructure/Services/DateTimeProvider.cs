using Chatty.BE.Application.Interfaces.Services;

namespace Chatty.BE.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
