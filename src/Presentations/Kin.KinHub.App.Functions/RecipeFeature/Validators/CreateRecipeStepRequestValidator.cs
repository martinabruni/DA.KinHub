using FluentValidation;

namespace Kin.KinHub.App.Functions.RecipeFeature;

internal sealed class CreateRecipeStepRequestValidator : AbstractValidator<CreateRecipeStepRequest>
{
    public CreateRecipeStepRequestValidator()
    {
        RuleFor(x => x.Order)
            .GreaterThan(0);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.RecipeId)
            .NotEmpty();
    }
}
