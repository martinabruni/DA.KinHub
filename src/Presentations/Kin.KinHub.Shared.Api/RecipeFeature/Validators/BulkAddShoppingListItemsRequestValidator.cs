using FluentValidation;

namespace Kin.KinHub.Shared.Api.RecipeFeature;

internal sealed class BulkAddShoppingListItemsRequestValidator : AbstractValidator<BulkAddShoppingListItemsRequest>
{
    public BulkAddShoppingListItemsRequestValidator()
    {
        RuleFor(x => x.Names)
            .NotNull()
            .Must(n => n.Count > 0).WithMessage("Names must contain at least one item.")
            .Must(n => n.All(name => !string.IsNullOrWhiteSpace(name))).WithMessage("Names must not contain empty values.");
        RuleFor(x => x.ShoppingListId)
            .NotEmpty();
    }
}
