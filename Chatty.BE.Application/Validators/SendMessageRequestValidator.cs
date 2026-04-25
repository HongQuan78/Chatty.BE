using FluentValidation;
using Chatty.BE.Application.DTOs.Messages;

namespace Chatty.BE.Application.Validators;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("ConversationId is required.");

        RuleFor(x => x.SenderId)
            .NotEmpty().WithMessage("SenderId is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(2000).WithMessage("Message content cannot exceed 2000 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid message type.");
    }
}
