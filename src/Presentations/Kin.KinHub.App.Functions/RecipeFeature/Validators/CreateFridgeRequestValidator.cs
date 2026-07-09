using FluentValidation;

namespace Kin.KinHub.App.Functions.RecipeFeature;

internal sealed class CreateFridgeRequestValidator : AbstractValidator<CreateFridgeRequest>
{
    public CreateFridgeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
