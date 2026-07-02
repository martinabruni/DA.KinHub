using FluentValidation;

namespace Kin.KinHub.KinRecipe.Api.RecipeFeature;

internal sealed class CreateFridgeRequestValidator : AbstractValidator<CreateFridgeRequest>
{
    public CreateFridgeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
