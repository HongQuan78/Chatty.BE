namespace Chatty.BE.API.Contracts.Auth;

public sealed record RegisterResponse(Guid Id, string UserName, string Email, string? DisplayName);
