using FluentValidation;

namespace Kin.KinHub.App.Functions.RecipeAssistantFeature;

internal sealed class ParseRecipeRequestValidator : AbstractValidator<ParseRecipeRequest>
{
    public ParseRecipeRequestValidator()
    {
        RuleFor(x => x.RawText).NotEmpty();
    }
}
