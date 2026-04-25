using Chatty.BE.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.BE.API.Extensions;

/// <summary>
/// Métodos de extensión para mapear Result a IActionResult de forma centralizada.
/// Evita la duplicación del patrón switch en cada controlador.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Convierte un Result sin valor a IActionResult según el código de error.
    /// </summary>
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.NoContent();
        }

        return MapErrorToResponse(result, controller);
    }

    /// <summary>
    /// Convierte un Result con valor a IActionResult usando un mapper para el caso de éxito.
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, IActionResult> onSuccess
    )
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        return MapErrorToResponse(result, controller);
    }

    private static IActionResult MapErrorToResponse(Result result, ControllerBase controller)
    {
        return result.ErrorCode switch
        {
            "NOT_FOUND" => controller.NotFound(new { error = result.Error }),
            "FORBIDDEN" => controller.Forbid(),
            "VALIDATION_ERROR" when result.ValidationErrors != null => 
                controller.ValidationProblem(new ValidationProblemDetails(result.ValidationErrors)),
            "BAD_REQUEST" => controller.BadRequest(new { error = result.Error }),
            "UNAUTHORIZED" => controller.Unauthorized(new { error = result.Error }),
            "CONFLICT" => controller.Conflict(new { error = result.Error }),
            _ => controller.StatusCode(500, new { error = result.Error }),
        };
    }
}
