using FluentValidation;

namespace Kin.KinHub.KinRecipe.Api.RecipeAssistantFeature;

internal sealed class SuggestRecipesRequestValidator : AbstractValidator<SuggestRecipesRequest>
{
    public SuggestRecipesRequestValidator()
    {
        RuleFor(x => x.FridgeId).NotEmpty();
    }
}
