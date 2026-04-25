using Chatty.BE.API.Contracts.Users;
using Chatty.BE.API.Extensions;
using Chatty.BE.Application.Interfaces.Services;
using Chatty.BE.Application.DTOs.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.BE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(IUserService userService, IPresenceService presenceService)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await userService.GetByIdAsync(id, ct);
        return result.ToActionResult(this, user => Ok(user));
    }

    [HttpGet("by-username/{userName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByUserName(string userName, CancellationToken ct)
    {
        var result = await userService.GetByUserNameAsync(userName, ct);
        return result.ToActionResult(this, user => Ok(user));
    }

    /// <summary>
    /// Searches for users with support for pagination and relevance optimization.
    /// </summary>
    /// <param name="keyword">Search term (matches UserName, Email, or DisplayName).</param>
    /// <param name="pageIndex">1-based page index.</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] string keyword,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Ok(new { items = Array.Empty<object>(), totalCount = 0 });
        }

        var result = await userService.SearchUsersAsync(keyword, pageIndex, pageSize, ct);
        return result.ToActionResult(this, pagedResult => Ok(pagedResult));
    }

    [HttpGet("{id:guid}/presence")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPresence(Guid id, CancellationToken ct)
    {
        var result = await presenceService.GetPresenceAsync(id, ct);
        return result.ToActionResult(this, presence => Ok(presence));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct
    )
    {
        var currentUserId = User.GetUserId();
        if (currentUserId != id)
        {
            return Forbid();
        }

        var result = await userService.UpdateProfileAsync(
            new UpdateUserProfileRequest
            {
                UserId = id,
                DisplayName = request.DisplayName,
                AvatarUrl = request.AvatarUrl,
                Bio = request.Bio
            },
            ct
        );

        return result.ToActionResult(this, user => Ok(user));
    }
}
