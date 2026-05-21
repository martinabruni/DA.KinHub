using FluentValidation;

namespace Kin.KinHub.Shared.Api.ChatFeature;

internal sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(4000);
    }
}
