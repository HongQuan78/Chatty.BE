using Chatty.BE.API.Contracts.Auth;
using Chatty.BE.API.Extensions;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.BE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken ct
    )
    {
        var user = await _authService.RegisterAsync(
            request.UserName,
            request.Email,
            request.Password,
            ct
        );

        var response = new RegisterResponse(user.Id, user.UserName, user.Email, user.DisplayName);

        return Ok(response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> LoginAsync(
        [FromBody] LoginRequestDto request,
        CancellationToken ct
    )
    {
        var response = await _authService.LoginAsync(request, HttpContext.GetClientIp(), ct);
        return Ok(response);
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        if (request.UserId != currentUserId)
        {
            return Forbid();
        }
        await _authService.ChangePasswordAsync(
            request.UserId,
            request.CurrentPassword,
            request.NewPassword,
            ct
        );

        return NoContent();
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] LogoutRequest request,
        CancellationToken ct
    )
    {
        await _authService.LogoutAsync(
            request.UserId,
            request.RefreshToken,
            HttpContext.GetClientIp(),
            ct
        );

        return NoContent();
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRefreshToken(
        RefreshTokenRequestDto refreshTokenRequestDto,
        CancellationToken ct
    )
    {
        var response = await _authService.RefreshAsync(
            refreshTokenRequestDto,
            HttpContext.GetClientIp(),
            ct
        );

        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout-all-sessions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LogoutAllSessionsAsync(
        [FromBody] LogoutAllSessionsRequest request,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        if (request.UserId != currentUserId)
        {
            return Forbid();
        }
        await _authService.LogoutAllSessionsAsync(request.UserId, HttpContext.GetClientIp(), ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActiveSessionsAsync(CancellationToken ct)
    {
        var currentUserId = User.GetUserId();
        var sessions = await _authService.GetActiveSessionsAsync(currentUserId, ct);
        return Ok(sessions);
    }
}
