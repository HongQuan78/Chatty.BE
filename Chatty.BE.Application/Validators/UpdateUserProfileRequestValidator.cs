using FluentValidation;
using Chatty.BE.Application.DTOs.Users;

namespace Chatty.BE.Application.Validators;

public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100).WithMessage("DisplayName cannot exceed 100 characters.")
            .When(x => x.DisplayName != null);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500).WithMessage("AvatarUrl cannot exceed 500 characters.")
            .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("AvatarUrl must be a valid absolute URI.")
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl));

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters.")
            .When(x => x.Bio != null);
    }
}
