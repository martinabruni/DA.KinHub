using FluentValidation;

namespace Kin.KinHub.KinRecipe.Api.RecipeFeature;

internal sealed class UpdateFridgeIngredientRequestValidator : AbstractValidator<UpdateFridgeIngredientRequest>
{
    public UpdateFridgeIngredientRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.MeasureUnit)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}
