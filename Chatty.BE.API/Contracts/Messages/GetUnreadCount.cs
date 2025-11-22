namespace Chatty.BE.API.Contracts.Messages;

public sealed record GetUnreadCount
{
    public int Count { get; init; }
}
