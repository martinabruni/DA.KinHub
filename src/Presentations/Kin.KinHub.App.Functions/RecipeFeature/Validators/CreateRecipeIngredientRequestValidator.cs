using FluentValidation;

namespace Kin.KinHub.App.Functions.RecipeFeature;

internal sealed class CreateRecipeIngredientRequestValidator : AbstractValidator<CreateRecipeIngredientRequest>
{
    public CreateRecipeIngredientRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.MeasureUnit)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.RecipeId)
            .NotEmpty();
    }
}
