using FluentValidation;

namespace Kin.KinHub.KinRecipe.Api.RecipeFeature;

internal sealed class UpdateRecipeStepRequestValidator : AbstractValidator<UpdateRecipeStepRequest>
{
    public UpdateRecipeStepRequestValidator()
    {
        RuleFor(x => x.Order)
            .GreaterThan(0);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
