using FluentValidation;

namespace Kin.KinHub.KinRecipe.Api.RecipeAssistantFeature;

internal sealed class AdaptRecipeRequestValidator : AbstractValidator<AdaptRecipeRequest>
{
    public AdaptRecipeRequestValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.Constraints).NotEmpty();
    }
}
