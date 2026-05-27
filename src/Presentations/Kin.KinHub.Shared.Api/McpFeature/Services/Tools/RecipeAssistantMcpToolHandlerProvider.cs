using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

[McpServerToolType]
public sealed class RecipeAssistantMcpTools : McpToolBase
{
    private readonly IRecipeAssistantManager _recipeAssistantManager;
    private readonly IRequestValidator<SuggestRecipesRequest> _suggestRecipesValidator;
    private readonly IRequestValidator<ParseRecipeRequest> _parseRecipeValidator;
    private readonly IRequestValidator<AdaptRecipeRequest> _adaptRecipeValidator;

    public RecipeAssistantMcpTools(
        ICurrentUser currentUser,
        IRecipeAssistantManager recipeAssistantManager,
        IRequestValidator<SuggestRecipesRequest> suggestRecipesValidator,
        IRequestValidator<ParseRecipeRequest> parseRecipeValidator,
        IRequestValidator<AdaptRecipeRequest> adaptRecipeValidator)
        : base(currentUser)
    {
        _recipeAssistantManager = recipeAssistantManager;
        _suggestRecipesValidator = suggestRecipesValidator;
        _parseRecipeValidator = parseRecipeValidator;
        _adaptRecipeValidator = adaptRecipeValidator;
    }

    [Authorize]
    [McpServerTool(Name = "recipe-assistant.suggest"), Description("Suggest recipes from a fridge.")]
    public async Task<CallToolResult> SuggestRecipesAsync(
        [Description("The fridge id used to suggest recipes.")] Guid fridgeId,
        CancellationToken cancellationToken) =>
        await ExecuteCoreValidatedAsync(
            new SuggestRecipesRequest
            {
                FridgeId = fridgeId,
            },
            _suggestRecipesValidator,
            async (payload, ct) => await _recipeAssistantManager.SuggestRecipesAsync(payload.FridgeId, CurrentUser.UserId, ct),
            cancellationToken);

    [Authorize]
    [McpServerTool(Name = "recipe-assistant.parse"), Description("Parse a recipe from free-form text.")]
    public async Task<CallToolResult> ParseRecipeAsync(
        [Description("The free-form recipe text to parse.")] string rawText,
        CancellationToken cancellationToken) =>
        await ExecuteCoreValidatedAsync(
            new ParseRecipeRequest
            {
                RawText = rawText,
            },
            _parseRecipeValidator,
            async (payload, ct) => await _recipeAssistantManager.ParseRecipeAsync(payload.RawText, ct),
            cancellationToken);

    [Authorize]
    [McpServerTool(Name = "recipe-assistant.adapt"), Description("Adapt a recipe to new constraints.")]
    public async Task<CallToolResult> AdaptRecipeAsync(
        [Description("The recipe id to adapt.")] Guid recipeId,
        [Description("The list of constraints to apply while adapting the recipe.")] IReadOnlyList<string> constraints,
        CancellationToken cancellationToken) =>
        await ExecuteCoreValidatedAsync(
            new AdaptRecipeRequest
            {
                RecipeId = recipeId,
                Constraints = constraints,
            },
            _adaptRecipeValidator,
            async (payload, ct) => await _recipeAssistantManager.AdaptRecipeAsync(payload.RecipeId, payload.Constraints, CurrentUser.UserId, ct),
            cancellationToken);
}
