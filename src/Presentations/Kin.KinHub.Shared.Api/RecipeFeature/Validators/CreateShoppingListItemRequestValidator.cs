using FluentValidation;

namespace Kin.KinHub.Shared.Api.RecipeFeature;

internal sealed class CreateShoppingListItemRequestValidator : AbstractValidator<CreateShoppingListItemRequest>
{
    public CreateShoppingListItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.ShoppingListId)
            .NotEmpty();
    }
}
