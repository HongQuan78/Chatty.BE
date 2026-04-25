using FluentValidation.Results;
using Chatty.BE.Application.Common;

namespace Chatty.BE.Application.Extensions;

public static class FluentValidationExtensions
{
    public static IDictionary<string, string[]> ToDictionary(this ValidationResult result)
    {
        return result.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage).ToArray()
            );
    }

    public static Result<T> ToResult<T>(this ValidationResult result)
    {
        return Result<T>.ValidationError(result.ToDictionary());
    }

    public static Result ToResult(this ValidationResult result)
    {
        return Result.ValidationError(result.ToDictionary());
    }
}
