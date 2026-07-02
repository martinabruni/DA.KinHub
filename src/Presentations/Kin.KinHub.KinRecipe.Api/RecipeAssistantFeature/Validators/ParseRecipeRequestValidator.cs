using FluentValidation;

namespace Kin.KinHub.KinRecipe.Api.RecipeAssistantFeature;

internal sealed class ParseRecipeRequestValidator : AbstractValidator<ParseRecipeRequest>
{
    public ParseRecipeRequestValidator()
    {
        RuleFor(x => x.RawText).NotEmpty();
    }
}
