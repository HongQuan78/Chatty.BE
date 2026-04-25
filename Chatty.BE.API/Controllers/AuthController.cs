using Chatty.BE.API.Contracts.Auth;
using Chatty.BE.API.Extensions;
using Chatty.BE.Application.DTOs.Auth;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppRegisterRequest = Chatty.BE.Application.DTOs.Auth.RegisterRequest;
using AppChangePasswordRequest = Chatty.BE.Application.DTOs.Auth.ChangePasswordRequest;

namespace Chatty.BE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] Chatty.BE.API.Contracts.Auth.RegisterRequest request,
        CancellationToken ct
    )
    {
        var result = await authService.RegisterAsync(
            new AppRegisterRequest
            {
                UserName = request.UserName,
                Email = request.Email,
                Password = request.Password
            },
            ct
        );

        return result.ToActionResult(this, user => Ok(new RegisterResponse(
            user.Id,
            user.UserName,
            user.Email,
            user.DisplayName
        )));
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequestDto request,
        CancellationToken ct
    )
    {
        var result = await authService.LoginAsync(request, HttpContext.GetClientIp(), ct);
        return result.ToActionResult(this, response => Ok(response));
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] Chatty.BE.API.Contracts.Auth.ChangePasswordRequest request,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        if (request.UserId != currentUserId)
        {
            return Forbid();
        }

        var result = await authService.ChangePasswordAsync(
            new AppChangePasswordRequest
            {
                UserId = request.UserId,
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            },
            ct
        );

        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LogoutAsync(
        [FromBody] LogoutRequest request,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        if (request.UserId != currentUserId)
        {
            return Forbid();
        }

        var result = await authService.LogoutAsync(
            request.UserId,
            request.RefreshToken,
            HttpContext.GetClientIp(),
            ct
        );

        return result.ToActionResult(this);
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
        var result = await authService.RefreshAsync(
            refreshTokenRequestDto,
            HttpContext.GetClientIp(),
            ct
        );

        return result.ToActionResult(this, response => Ok(response));
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

        var result = await authService.LogoutAllSessionsAsync(request.UserId, HttpContext.GetClientIp(), ct);
        return result.ToActionResult(this);
    }

    [Authorize]
    [HttpGet("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActiveSessionsAsync(CancellationToken ct)
    {
        var currentUserId = User.GetUserId();
        var result = await authService.GetActiveSessionsAsync(currentUserId, ct);
        return result.ToActionResult(this, sessions => Ok(sessions));
    }
}
