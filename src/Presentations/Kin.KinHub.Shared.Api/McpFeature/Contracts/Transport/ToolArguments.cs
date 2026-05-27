namespace Kin.KinHub.Shared.Api.McpFeature.Contracts.Transport;

public sealed class AccountToolArguments
{
    public required string Action { get; init; }
    public UpdateUserEmailRequest? UpdateEmail { get; init; }
    public UpdateUserPasswordRequest? UpdatePassword { get; init; }
}

public sealed class FamilyToolArguments
{
    public required string Action { get; init; }
    public Guid? FamilyId { get; init; }
    public CreateFamilyRequest? Create { get; init; }
    public UpdateFamilyRequest? Update { get; init; }
}

public sealed class FamilyMemberToolArguments
{
    public required string Action { get; init; }
    public required Guid FamilyId { get; init; }
    public Guid? MemberId { get; init; }
    public AddFamilyMemberRequest? Add { get; init; }
    public UpdateFamilyMemberRequest? Update { get; init; }
}

public sealed class FamilyServiceToolArguments
{
    public required Guid FamilyId { get; init; }
    public required ToggleFamilyServiceRequest Request { get; init; }
}

public sealed class RecipeBookToolArguments
{
    public required string Action { get; init; }
    public Guid? Id { get; init; }
    public CreateRecipeBookRequest? Create { get; init; }
    public UpdateRecipeBookRequest? Update { get; init; }
}

public sealed class RecipeToolArguments
{
    public required string Action { get; init; }
    public Guid? RecipeBookId { get; init; }
    public Guid? RecipeId { get; init; }
    public Guid? FridgeId { get; init; }
    public CreateRecipeRequest? Create { get; init; }
    public UpdateRecipeRequest? Update { get; init; }
}

public sealed class RecipeIngredientToolArguments
{
    public required string Action { get; init; }
    public Guid? RecipeId { get; init; }
    public Guid? IngredientId { get; init; }
    public CreateRecipeIngredientRequest? Create { get; init; }
    public UpdateRecipeIngredientRequest? Update { get; init; }
}

public sealed class RecipeStepToolArguments
{
    public required string Action { get; init; }
    public Guid? RecipeId { get; init; }
    public Guid? StepId { get; init; }
    public CreateRecipeStepRequest? Create { get; init; }
    public UpdateRecipeStepRequest? Update { get; init; }
}

public sealed class FridgeToolArguments
{
    public required string Action { get; init; }
    public Guid? Id { get; init; }
    public CreateFridgeRequest? Create { get; init; }
    public UpdateFridgeRequest? Update { get; init; }
}

public sealed class FridgeIngredientToolArguments
{
    public required string Action { get; init; }
    public Guid? FridgeId { get; init; }
    public Guid? IngredientId { get; init; }
    public CreateFridgeIngredientRequest? Create { get; init; }
    public UpdateFridgeIngredientRequest? Update { get; init; }
}

public sealed class ShoppingListToolArguments
{
    public required string Action { get; init; }
    public Guid? Id { get; init; }
    public CreateShoppingListRequest? Create { get; init; }
    public UpdateShoppingListRequest? Update { get; init; }
}

public sealed class ShoppingListItemToolArguments
{
    public required string Action { get; init; }
    public required Guid ListId { get; init; }
    public Guid? ItemId { get; init; }
    public CreateShoppingListItemRequest? Add { get; init; }
    public BulkAddShoppingListItemsRequest? BulkAdd { get; init; }
}
