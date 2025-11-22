using Chatty.BE.API.Contracts.Users;
using Chatty.BE.API.Extensions;
using Chatty.BE.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.BE.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(id, ct);
        if (user is null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpGet("by-username/{userName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByUserName(string userName, CancellationToken ct)
    {
        var user = await userService.GetByUserNameAsync(userName, ct);
        if (user is null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] string keyword, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Ok(Array.Empty<object>());
        }

        var users = await userService.SearchUsersAsync(keyword, ct);
        return Ok(users);
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

        var updated = await userService.UpdateProfileAsync(
            id,
            request.DisplayName,
            request.AvatarUrl,
            request.Bio,
            ct
        );

        return Ok(updated);
    }
}
