using FluentValidation;

namespace Kin.KinHub.App.Functions.RecipeFeature;

internal sealed class UpdateFridgeRequestValidator : AbstractValidator<UpdateFridgeRequest>
{
    public UpdateFridgeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
