using FluentValidation;

namespace Kin.KinHub.App.Functions.RecipeAssistantFeature;

internal sealed class AdaptRecipeRequestValidator : AbstractValidator<AdaptRecipeRequest>
{
    public AdaptRecipeRequestValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.Constraints).NotEmpty();
    }
}
