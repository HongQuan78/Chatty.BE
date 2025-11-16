namespace Chatty.BE.Application.Interfaces.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
