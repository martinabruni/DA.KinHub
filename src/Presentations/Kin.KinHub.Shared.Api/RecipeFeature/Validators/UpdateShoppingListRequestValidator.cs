using FluentValidation;

namespace Kin.KinHub.Shared.Api.RecipeFeature;

internal sealed class UpdateShoppingListRequestValidator : AbstractValidator<UpdateShoppingListRequest>
{
    public UpdateShoppingListRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
