using FluentValidation;

namespace Kin.KinHub.App.Functions.RecipeAssistantFeature;

internal sealed class SuggestRecipesRequestValidator : AbstractValidator<SuggestRecipesRequest>
{
    public SuggestRecipesRequestValidator()
    {
        RuleFor(x => x.FridgeId).NotEmpty();
    }
}
