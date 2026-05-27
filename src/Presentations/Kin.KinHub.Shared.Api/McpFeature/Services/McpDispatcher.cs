using Kin.KinHub.Shared.Api.McpFeature.Contracts;
using System.Text.Json;
using CoreResult = Kin.KinHub.Core.Business.Common;
using IdentityResult = Kin.KinHub.Identity.Business.Common;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

internal sealed class McpDispatcher : IMcpDispatcher
{
    private readonly IMcpSessionService _sessionService;
    private readonly McpTransportOptions _options;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthenticationService _authenticationService;
    private readonly IFamilyService _familyService;
    private readonly IKinHubServiceService _kinHubServiceService;
    private readonly IRecipeBookService _recipeBookService;
    private readonly IRecipeService _recipeService;
    private readonly IRecipeIngredientService _recipeIngredientService;
    private readonly IRecipeStepService _recipeStepService;
    private readonly IFridgeService _fridgeService;
    private readonly IFridgeIngredientService _fridgeIngredientService;
    private readonly IShoppingListService _shoppingListService;
    private readonly IShoppingListItemService _shoppingListItemService;
    private readonly IRecipeAssistantManager _recipeAssistantManager;
    private readonly IRecipeMissingIngredientsService _recipeMissingIngredientsService;
    private readonly IRequestValidator<LoginRequest> _loginValidator;
    private readonly IRequestValidator<RegisterRequest> _registerValidator;
    private readonly IRequestValidator<RefreshRequest> _refreshValidator;
    private readonly IRequestValidator<UpdateUserEmailRequest> _updateEmailValidator;
    private readonly IRequestValidator<UpdateUserPasswordRequest> _updatePasswordValidator;
    private readonly IRequestValidator<CreateFamilyRequest> _createFamilyValidator;
    private readonly IRequestValidator<AddFamilyMemberRequest> _addFamilyMemberValidator;
    private readonly IRequestValidator<UpdateFamilyMemberRequest> _updateFamilyMemberValidator;
    private readonly IRequestValidator<UpdateFamilyRequest> _updateFamilyValidator;
    private readonly IRequestValidator<ToggleFamilyServiceRequest> _toggleFamilyServiceValidator;
    private readonly IRequestValidator<CreateRecipeBookRequest> _createRecipeBookValidator;
    private readonly IRequestValidator<UpdateRecipeBookRequest> _updateRecipeBookValidator;
    private readonly IRequestValidator<CreateRecipeRequest> _createRecipeValidator;
    private readonly IRequestValidator<UpdateRecipeRequest> _updateRecipeValidator;
    private readonly IRequestValidator<CreateRecipeIngredientRequest> _createRecipeIngredientValidator;
    private readonly IRequestValidator<UpdateRecipeIngredientRequest> _updateRecipeIngredientValidator;
    private readonly IRequestValidator<CreateRecipeStepRequest> _createRecipeStepValidator;
    private readonly IRequestValidator<UpdateRecipeStepRequest> _updateRecipeStepValidator;
    private readonly IRequestValidator<CreateFridgeRequest> _createFridgeValidator;
    private readonly IRequestValidator<UpdateFridgeRequest> _updateFridgeValidator;
    private readonly IRequestValidator<CreateFridgeIngredientRequest> _createFridgeIngredientValidator;
    private readonly IRequestValidator<UpdateFridgeIngredientRequest> _updateFridgeIngredientValidator;
    private readonly IRequestValidator<CreateShoppingListRequest> _createShoppingListValidator;
    private readonly IRequestValidator<UpdateShoppingListRequest> _updateShoppingListValidator;
    private readonly IRequestValidator<CreateShoppingListItemRequest> _createShoppingListItemValidator;
    private readonly IRequestValidator<BulkAddShoppingListItemsRequest> _bulkAddShoppingListItemsValidator;
    private readonly IRequestValidator<SuggestRecipesRequest> _suggestRecipesValidator;
    private readonly IRequestValidator<ParseRecipeRequest> _parseRecipeValidator;
    private readonly IRequestValidator<AdaptRecipeRequest> _adaptRecipeValidator;
    private readonly IReadOnlyDictionary<string, McpToolRegistration> _tools;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public McpDispatcher(
        IMcpSessionService sessionService,
        McpTransportOptions options,
        ICurrentUser currentUser,
        IAuthenticationService authenticationService,
        IFamilyService familyService,
        IKinHubServiceService kinHubServiceService,
        IRecipeBookService recipeBookService,
        IRecipeService recipeService,
        IRecipeIngredientService recipeIngredientService,
        IRecipeStepService recipeStepService,
        IFridgeService fridgeService,
        IFridgeIngredientService fridgeIngredientService,
        IShoppingListService shoppingListService,
        IShoppingListItemService shoppingListItemService,
        IRecipeAssistantManager recipeAssistantManager,
        IRecipeMissingIngredientsService recipeMissingIngredientsService,
        IRequestValidator<LoginRequest> loginValidator,
        IRequestValidator<RegisterRequest> registerValidator,
        IRequestValidator<RefreshRequest> refreshValidator,
        IRequestValidator<UpdateUserEmailRequest> updateEmailValidator,
        IRequestValidator<UpdateUserPasswordRequest> updatePasswordValidator,
        IRequestValidator<CreateFamilyRequest> createFamilyValidator,
        IRequestValidator<AddFamilyMemberRequest> addFamilyMemberValidator,
        IRequestValidator<UpdateFamilyMemberRequest> updateFamilyMemberValidator,
        IRequestValidator<UpdateFamilyRequest> updateFamilyValidator,
        IRequestValidator<ToggleFamilyServiceRequest> toggleFamilyServiceValidator,
        IRequestValidator<CreateRecipeBookRequest> createRecipeBookValidator,
        IRequestValidator<UpdateRecipeBookRequest> updateRecipeBookValidator,
        IRequestValidator<CreateRecipeRequest> createRecipeValidator,
        IRequestValidator<UpdateRecipeRequest> updateRecipeValidator,
        IRequestValidator<CreateRecipeIngredientRequest> createRecipeIngredientValidator,
        IRequestValidator<UpdateRecipeIngredientRequest> updateRecipeIngredientValidator,
        IRequestValidator<CreateRecipeStepRequest> createRecipeStepValidator,
        IRequestValidator<UpdateRecipeStepRequest> updateRecipeStepValidator,
        IRequestValidator<CreateFridgeRequest> createFridgeValidator,
        IRequestValidator<UpdateFridgeRequest> updateFridgeValidator,
        IRequestValidator<CreateFridgeIngredientRequest> createFridgeIngredientValidator,
        IRequestValidator<UpdateFridgeIngredientRequest> updateFridgeIngredientValidator,
        IRequestValidator<CreateShoppingListRequest> createShoppingListValidator,
        IRequestValidator<UpdateShoppingListRequest> updateShoppingListValidator,
        IRequestValidator<CreateShoppingListItemRequest> createShoppingListItemValidator,
        IRequestValidator<BulkAddShoppingListItemsRequest> bulkAddShoppingListItemsValidator,
        IRequestValidator<SuggestRecipesRequest> suggestRecipesValidator,
        IRequestValidator<ParseRecipeRequest> parseRecipeValidator,
        IRequestValidator<AdaptRecipeRequest> adaptRecipeValidator)
    {
        _sessionService = sessionService;
        _options = options;
        _currentUser = currentUser;
        _authenticationService = authenticationService;
        _familyService = familyService;
        _kinHubServiceService = kinHubServiceService;
        _recipeBookService = recipeBookService;
        _recipeService = recipeService;
        _recipeIngredientService = recipeIngredientService;
        _recipeStepService = recipeStepService;
        _fridgeService = fridgeService;
        _fridgeIngredientService = fridgeIngredientService;
        _shoppingListService = shoppingListService;
        _shoppingListItemService = shoppingListItemService;
        _recipeAssistantManager = recipeAssistantManager;
        _recipeMissingIngredientsService = recipeMissingIngredientsService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _refreshValidator = refreshValidator;
        _updateEmailValidator = updateEmailValidator;
        _updatePasswordValidator = updatePasswordValidator;
        _createFamilyValidator = createFamilyValidator;
        _addFamilyMemberValidator = addFamilyMemberValidator;
        _updateFamilyMemberValidator = updateFamilyMemberValidator;
        _updateFamilyValidator = updateFamilyValidator;
        _toggleFamilyServiceValidator = toggleFamilyServiceValidator;
        _createRecipeBookValidator = createRecipeBookValidator;
        _updateRecipeBookValidator = updateRecipeBookValidator;
        _createRecipeValidator = createRecipeValidator;
        _updateRecipeValidator = updateRecipeValidator;
        _createRecipeIngredientValidator = createRecipeIngredientValidator;
        _updateRecipeIngredientValidator = updateRecipeIngredientValidator;
        _createRecipeStepValidator = createRecipeStepValidator;
        _updateRecipeStepValidator = updateRecipeStepValidator;
        _createFridgeValidator = createFridgeValidator;
        _updateFridgeValidator = updateFridgeValidator;
        _createFridgeIngredientValidator = createFridgeIngredientValidator;
        _updateFridgeIngredientValidator = updateFridgeIngredientValidator;
        _createShoppingListValidator = createShoppingListValidator;
        _updateShoppingListValidator = updateShoppingListValidator;
        _createShoppingListItemValidator = createShoppingListItemValidator;
        _bulkAddShoppingListItemsValidator = bulkAddShoppingListItemsValidator;
        _suggestRecipesValidator = suggestRecipesValidator;
        _parseRecipeValidator = parseRecipeValidator;
        _adaptRecipeValidator = adaptRecipeValidator;
        _tools = CreateToolRegistry();
    }

    public async Task<McpDispatchResult> DispatchAsync(McpJsonRpcMessage message, string? sessionId, CancellationToken cancellationToken)
    {
        return message.Method switch
        {
            "initialize" => await HandleInitializeAsync(message, cancellationToken),
            "notifications/initialized" => HandleInitializedNotification(sessionId),
            "ping" => new McpDispatchResult
            {
                Response = new McpJsonRpcSuccessResponse
                {
                    Id = message.Id,
                    Result = new { },
                },
            },
            "tools/list" => new McpDispatchResult
            {
                Response = new McpJsonRpcSuccessResponse
                {
                    Id = message.Id,
                    Result = new McpToolListResult
                    {
                        Tools = _tools.Values.Select(static tool => tool.Definition).ToArray(),
                    },
                },
            },
            "tools/call" => new McpDispatchResult
            {
                Response = await HandleToolCallAsync(message, cancellationToken),
            },
            _ => new McpDispatchResult
            {
                Response = McpErrorMapper.JsonRpcError(message.Id, McpErrorMapper.MethodNotFound, $"Unknown MCP method '{message.Method}'."),
            },
        };
    }

    private async Task<McpDispatchResult> HandleInitializeAsync(McpJsonRpcMessage message, CancellationToken cancellationToken)
    {
        var request = message.Params?.Deserialize(McpJsonSerializerContext.Default.McpInitializeRequestParams);
        if (request is null)
        {
            return new McpDispatchResult
            {
                Response = McpErrorMapper.JsonRpcError(message.Id, McpErrorMapper.InvalidParams, "Invalid initialize payload."),
            };
        }

        await Task.CompletedTask.ConfigureAwait(false);

        var session = _sessionService.CreateSession(request);
        return new McpDispatchResult
        {
            CreatedSessionId = session.Id,
            Response = new McpJsonRpcSuccessResponse
            {
                Id = message.Id,
                Result = new McpInitializeResult
                {
                    ProtocolVersion = _options.ProtocolVersion,
                    Capabilities = new McpServerCapabilities
                    {
                        Tools = new McpToolsCapability
                        {
                            ListChanged = false,
                        },
                    },
                    ServerInfo = new McpServerInfo
                    {
                        Name = _options.ServerName,
                        Version = _options.ServerVersion,
                    },
                    Instructions = _options.Instructions,
                },
            },
        };
    }

    private McpDispatchResult HandleInitializedNotification(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !_sessionService.TryMarkInitialized(sessionId, out _))
        {
            return new McpDispatchResult
            {
                Response = McpErrorMapper.JsonRpcError(null, McpErrorMapper.InvalidRequest, "Mcp-Session-Id is required for notifications/initialized."),
            };
        }

        return new McpDispatchResult();
    }

    private async Task<McpJsonRpcResponse> HandleToolCallAsync(McpJsonRpcMessage message, CancellationToken cancellationToken)
    {
        var request = message.Params?.Deserialize(McpJsonSerializerContext.Default.McpToolCallParams);
        if (request is null)
        {
            return McpErrorMapper.JsonRpcError(message.Id, McpErrorMapper.InvalidParams, "Invalid tools/call payload.");
        }

        if (!_tools.TryGetValue(request.Name, out var tool))
        {
            return McpErrorMapper.JsonRpcError(message.Id, McpErrorMapper.InvalidParams, $"Unknown tool '{request.Name}'.");
        }

        try
        {
            var result = await tool.ExecuteAsync(request.Arguments, cancellationToken);
            return new McpJsonRpcSuccessResponse
            {
                Id = message.Id,
                Result = result,
            };
        }
        catch (JsonException)
        {
            return McpErrorMapper.JsonRpcError(message.Id, McpErrorMapper.InvalidParams, $"Invalid arguments for tool '{request.Name}'.");
        }
        catch (InvalidOperationException ex)
        {
            return McpErrorMapper.JsonRpcError(message.Id, McpErrorMapper.InvalidParams, ex.Message);
        }
        catch (Exception ex)
        {
            return McpErrorMapper.JsonRpcError(message.Id, McpErrorMapper.InternalError, ex.Message);
        }
    }

    private IReadOnlyDictionary<string, McpToolRegistration> CreateToolRegistry() =>
        new Dictionary<string, McpToolRegistration>(StringComparer.Ordinal)
        {
            ["auth.login"] = CreateTool<LoginRequest>("Authenticate with email and password.", HandleLoginAsync),
            ["auth.register"] = CreateTool<RegisterRequest>("Register a new KinHub user.", HandleRegisterAsync),
            ["auth.refresh"] = CreateTool<RefreshRequest>("Refresh an access token using a refresh token.", HandleRefreshAsync),
            ["auth.logout"] = CreateTool<RefreshRequest>("Revoke a refresh token.", HandleLogoutAsync),
            ["auth.account"] = CreateTool<AccountToolArguments>("Read or update the current account.", HandleAccountAsync),
            ["family.manage"] = CreateTool<FamilyToolArguments>("Create, read, update, or delete the current family.", HandleFamilyAsync),
            ["family.member.manage"] = CreateTool<FamilyMemberToolArguments>("Add, update, or delete a family member.", HandleFamilyMemberAsync),
            ["family.services.list"] = CreateTool<object>("List all KinHub services.", HandleListServicesAsync),
            ["family.services.get"] = CreateTool<GuidEnvelope>("Get the enabled services for a family.", HandleGetFamilyServicesAsync),
            ["family.services.toggle"] = CreateTool<FamilyServiceToolArguments>("Enable or disable a KinHub service for a family.", HandleToggleFamilyServiceAsync),
            ["recipe-book.manage"] = CreateTool<RecipeBookToolArguments>("Create, list, read, update, or delete recipe books.", HandleRecipeBookAsync),
            ["recipe.manage"] = CreateTool<RecipeToolArguments>("Create, list, read, update, delete recipes, or compute missing ingredients.", HandleRecipeAsync),
            ["recipe.ingredient.manage"] = CreateTool<RecipeIngredientToolArguments>("Create, list, read, update, or delete recipe ingredients.", HandleRecipeIngredientAsync),
            ["recipe.step.manage"] = CreateTool<RecipeStepToolArguments>("Create, list, read, update, or delete recipe steps.", HandleRecipeStepAsync),
            ["fridge.manage"] = CreateTool<FridgeToolArguments>("Create, list, read, update, or delete fridges.", HandleFridgeAsync),
            ["fridge.ingredient.manage"] = CreateTool<FridgeIngredientToolArguments>("Create, list, read, update, or delete fridge ingredients.", HandleFridgeIngredientAsync),
            ["shopping-list.manage"] = CreateTool<ShoppingListToolArguments>("Create, list, update, or delete shopping lists.", HandleShoppingListAsync),
            ["shopping-list.item.manage"] = CreateTool<ShoppingListItemToolArguments>("List, add, bulk add, toggle, or delete shopping list items.", HandleShoppingListItemAsync),
            ["recipe-assistant.suggest"] = CreateTool<SuggestRecipesRequest>("Suggest recipes from a fridge.", HandleSuggestRecipesAsync),
            ["recipe-assistant.parse"] = CreateTool<ParseRecipeRequest>("Parse a recipe from free-form text.", HandleParseRecipeAsync),
            ["recipe-assistant.adapt"] = CreateTool<AdaptRecipeRequest>("Adapt a recipe to new constraints.", HandleAdaptRecipeAsync),
        };

    private McpToolRegistration CreateTool<TArguments>(
        string description,
        Func<JsonElement, CancellationToken, Task<McpToolCallResult>> handler) =>
        new(
            new McpToolDefinition
            {
                Name = GetToolName(handler),
                Description = description,
                InputSchema = McpInputSchemaGenerator.Generate(typeof(TArguments)),
            },
            handler);

    private static string GetToolName(Delegate handler)
    {
        var name = handler.Method.Name;
        return name switch
        {
            nameof(HandleLoginAsync) => "auth.login",
            nameof(HandleRegisterAsync) => "auth.register",
            nameof(HandleRefreshAsync) => "auth.refresh",
            nameof(HandleLogoutAsync) => "auth.logout",
            nameof(HandleAccountAsync) => "auth.account",
            nameof(HandleFamilyAsync) => "family.manage",
            nameof(HandleFamilyMemberAsync) => "family.member.manage",
            nameof(HandleListServicesAsync) => "family.services.list",
            nameof(HandleGetFamilyServicesAsync) => "family.services.get",
            nameof(HandleToggleFamilyServiceAsync) => "family.services.toggle",
            nameof(HandleRecipeBookAsync) => "recipe-book.manage",
            nameof(HandleRecipeAsync) => "recipe.manage",
            nameof(HandleRecipeIngredientAsync) => "recipe.ingredient.manage",
            nameof(HandleRecipeStepAsync) => "recipe.step.manage",
            nameof(HandleFridgeAsync) => "fridge.manage",
            nameof(HandleFridgeIngredientAsync) => "fridge.ingredient.manage",
            nameof(HandleShoppingListAsync) => "shopping-list.manage",
            nameof(HandleShoppingListItemAsync) => "shopping-list.item.manage",
            nameof(HandleSuggestRecipesAsync) => "recipe-assistant.suggest",
            nameof(HandleParseRecipeAsync) => "recipe-assistant.parse",
            nameof(HandleAdaptRecipeAsync) => "recipe-assistant.adapt",
            _ => throw new InvalidOperationException($"Unknown tool handler '{name}'."),
        };
    }

    private async Task<McpToolCallResult> HandleLoginAsync(JsonElement arguments, CancellationToken cancellationToken) =>
        await ExecuteIdentityValidatedAsync(arguments, _loginValidator, _authenticationService.LoginAsync, cancellationToken);

    private async Task<McpToolCallResult> HandleRegisterAsync(JsonElement arguments, CancellationToken cancellationToken) =>
        await ExecuteIdentityValidatedAsync(arguments, _registerValidator, _authenticationService.RegisterAsync, cancellationToken);

    private async Task<McpToolCallResult> HandleRefreshAsync(JsonElement arguments, CancellationToken cancellationToken) =>
        await ExecuteIdentityValidatedAsync(arguments, _refreshValidator, async (request, ct) =>
            await _authenticationService.RefreshTokenAsync(request.RefreshToken, ct), cancellationToken);

    private async Task<McpToolCallResult> HandleLogoutAsync(JsonElement arguments, CancellationToken cancellationToken) =>
        await ExecuteIdentityValidatedAsync(arguments, _refreshValidator, async (request, ct) =>
            await _authenticationService.LogoutAsync(request.RefreshToken, ct), cancellationToken);

    private async Task<McpToolCallResult> HandleAccountAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<AccountToolArguments>(arguments);
        return request.Action switch
        {
            "get" => McpErrorMapper.FromIdentityResult(await _authenticationService.GetCurrentUserAsync(_currentUser.UserId, cancellationToken)),
            "update-email" => await ExecuteIdentityValidatedAsync(request.UpdateEmail, _updateEmailValidator, async (payload, ct) =>
                await _authenticationService.UpdateUserEmailAsync(_currentUser.UserId, payload, ct), cancellationToken),
            "update-password" => await ExecuteIdentityValidatedAsync(request.UpdatePassword, _updatePasswordValidator, async (payload, ct) =>
                await _authenticationService.UpdateUserPasswordAsync(_currentUser.UserId, payload, ct), cancellationToken),
            "delete" => McpErrorMapper.FromIdentityResult(await _authenticationService.DeleteUserAsync(_currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported account action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleFamilyAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<FamilyToolArguments>(arguments);
        return request.Action switch
        {
            "create" => await ExecuteCoreValidatedAsync(request.Create, _createFamilyValidator, async (payload, ct) =>
                await _familyService.CreateFamilyAsync(payload, _currentUser.UserId, ct), cancellationToken),
            "get" => McpErrorMapper.FromCoreResult(await _familyService.GetFamilyAsync(_currentUser.UserId, cancellationToken)),
            "update" => await ExecuteCoreValidatedAsync(request.Update, _updateFamilyValidator, async (payload, ct) =>
                await _familyService.UpdateFamilyAsync(RequireGuid(request.FamilyId, nameof(request.FamilyId)), payload, _currentUser.UserId, ct), cancellationToken),
            "delete" => McpErrorMapper.FromCoreResult(await _familyService.DeleteFamilyAsync(RequireGuid(request.FamilyId, nameof(request.FamilyId)), _currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported family action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleFamilyMemberAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<FamilyMemberToolArguments>(arguments);
        return request.Action switch
        {
            "add" => await ExecuteCoreValidatedAsync(request.Add, _addFamilyMemberValidator, async (payload, ct) =>
                await _familyService.AddFamilyMemberAsync(request.FamilyId, payload, _currentUser.UserId, ct), cancellationToken),
            "update" => await ExecuteCoreValidatedAsync(request.Update, _updateFamilyMemberValidator, async (payload, ct) =>
                await _familyService.UpdateFamilyMemberAsync(request.FamilyId, RequireGuid(request.MemberId, nameof(request.MemberId)), payload, _currentUser.UserId, ct), cancellationToken),
            "delete" => McpErrorMapper.FromCoreResult(await _familyService.DeleteFamilyMemberAsync(request.FamilyId, RequireGuid(request.MemberId, nameof(request.MemberId)), _currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported family member action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleListServicesAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        _ = arguments;

        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        return McpErrorMapper.FromCoreResult(await _kinHubServiceService.GetAllServicesAsync(cancellationToken));
    }

    private async Task<McpToolCallResult> HandleGetFamilyServicesAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<GuidEnvelope>(arguments);
        return McpErrorMapper.FromCoreResult(await _kinHubServiceService.GetFamilyServicesAsync(request.Id, cancellationToken));
    }

    private async Task<McpToolCallResult> HandleToggleFamilyServiceAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<FamilyServiceToolArguments>(arguments);
        return await ExecuteCoreValidatedAsync(request.Request, _toggleFamilyServiceValidator, async (payload, ct) =>
            await _kinHubServiceService.ToggleFamilyServiceAsync(request.FamilyId, payload, ct), cancellationToken);
    }

    private async Task<McpToolCallResult> HandleRecipeBookAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<RecipeBookToolArguments>(arguments);
        return request.Action switch
        {
            "create" => await ExecuteCoreValidatedAsync(request.Create, _createRecipeBookValidator, async (payload, ct) =>
                await _recipeBookService.CreateAsync(payload, _currentUser.UserId, ct), cancellationToken),
            "list" => McpErrorMapper.FromCoreResult(await _recipeBookService.GetAllAsync(_currentUser.UserId, cancellationToken)),
            "get" => McpErrorMapper.FromCoreResult(await _recipeBookService.GetByIdAsync(RequireGuid(request.Id, nameof(request.Id)), _currentUser.UserId, cancellationToken)),
            "update" => await ExecuteCoreValidatedAsync(request.Update, _updateRecipeBookValidator, async (payload, ct) =>
                await _recipeBookService.UpdateAsync(RequireGuid(request.Id, nameof(request.Id)), payload, _currentUser.UserId, ct), cancellationToken),
            "delete" => McpErrorMapper.FromCoreResult(await _recipeBookService.DeleteAsync(RequireGuid(request.Id, nameof(request.Id)), _currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported recipe-book action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleRecipeAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<RecipeToolArguments>(arguments);
        return request.Action switch
        {
            "create" => await ExecuteCoreValidatedAsync(request.Create, _createRecipeValidator, async (payload, ct) =>
                await _recipeService.CreateAsync(payload, _currentUser.UserId, ct), cancellationToken),
            "list" => McpErrorMapper.FromCoreResult(await _recipeService.GetAllAsync(RequireGuid(request.RecipeBookId, nameof(request.RecipeBookId)), _currentUser.UserId, cancellationToken)),
            "get" => McpErrorMapper.FromCoreResult(await _recipeService.GetByIdAsync(RequireGuid(request.RecipeId, nameof(request.RecipeId)), _currentUser.UserId, cancellationToken)),
            "update" => await ExecuteCoreValidatedAsync(request.Update, _updateRecipeValidator, async (payload, ct) =>
                await _recipeService.UpdateAsync(RequireGuid(request.RecipeId, nameof(request.RecipeId)), payload, _currentUser.UserId, ct), cancellationToken),
            "delete" => McpErrorMapper.FromCoreResult(await _recipeService.DeleteAsync(RequireGuid(request.RecipeId, nameof(request.RecipeId)), _currentUser.UserId, cancellationToken)),
            "missing-ingredients" => McpErrorMapper.ToolSuccess(new
            {
                missingIngredients = await _recipeMissingIngredientsService.GetMissingIngredientsAsync(
                    RequireGuid(request.RecipeId, nameof(request.RecipeId)),
                    RequireGuid(request.FridgeId, nameof(request.FridgeId)),
                    cancellationToken),
            }),
            _ => throw new InvalidOperationException($"Unsupported recipe action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleRecipeIngredientAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<RecipeIngredientToolArguments>(arguments);
        return request.Action switch
        {
            "create" => await ExecuteCoreValidatedAsync(request.Create, _createRecipeIngredientValidator, async (payload, ct) =>
                await _recipeIngredientService.CreateAsync(payload, _currentUser.UserId, ct), cancellationToken),
            "list" => McpErrorMapper.FromCoreResult(await _recipeIngredientService.GetAllAsync(RequireGuid(request.RecipeId, nameof(request.RecipeId)), _currentUser.UserId, cancellationToken)),
            "get" => McpErrorMapper.FromCoreResult(await _recipeIngredientService.GetByIdAsync(RequireGuid(request.IngredientId, nameof(request.IngredientId)), _currentUser.UserId, cancellationToken)),
            "update" => await ExecuteCoreValidatedAsync(request.Update, _updateRecipeIngredientValidator, async (payload, ct) =>
                await _recipeIngredientService.UpdateAsync(RequireGuid(request.IngredientId, nameof(request.IngredientId)), payload, _currentUser.UserId, ct), cancellationToken),
            "delete" => McpErrorMapper.FromCoreResult(await _recipeIngredientService.DeleteAsync(RequireGuid(request.IngredientId, nameof(request.IngredientId)), _currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported recipe ingredient action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleRecipeStepAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<RecipeStepToolArguments>(arguments);
        return request.Action switch
        {
            "create" => await ExecuteCoreValidatedAsync(request.Create, _createRecipeStepValidator, async (payload, ct) =>
                await _recipeStepService.CreateAsync(payload, _currentUser.UserId, ct), cancellationToken),
            "list" => McpErrorMapper.FromCoreResult(await _recipeStepService.GetAllAsync(RequireGuid(request.RecipeId, nameof(request.RecipeId)), _currentUser.UserId, cancellationToken)),
            "get" => McpErrorMapper.FromCoreResult(await _recipeStepService.GetByIdAsync(RequireGuid(request.StepId, nameof(request.StepId)), _currentUser.UserId, cancellationToken)),
            "update" => await ExecuteCoreValidatedAsync(request.Update, _updateRecipeStepValidator, async (payload, ct) =>
                await _recipeStepService.UpdateAsync(RequireGuid(request.StepId, nameof(request.StepId)), payload, _currentUser.UserId, ct), cancellationToken),
            "delete" => McpErrorMapper.FromCoreResult(await _recipeStepService.DeleteAsync(RequireGuid(request.StepId, nameof(request.StepId)), _currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported recipe step action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleFridgeAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<FridgeToolArguments>(arguments);
        return request.Action switch
        {
            "create" => await ExecuteCoreValidatedAsync(request.Create, _createFridgeValidator, async (payload, ct) =>
                await _fridgeService.CreateAsync(payload, _currentUser.UserId, ct), cancellationToken),
            "list" => McpErrorMapper.FromCoreResult(await _fridgeService.GetAllAsync(_currentUser.UserId, cancellationToken)),
            "get" => McpErrorMapper.FromCoreResult(await _fridgeService.GetByIdAsync(RequireGuid(request.Id, nameof(request.Id)), _currentUser.UserId, cancellationToken)),
            "update" => await ExecuteCoreValidatedAsync(request.Update, _updateFridgeValidator, async (payload, ct) =>
                await _fridgeService.UpdateAsync(RequireGuid(request.Id, nameof(request.Id)), payload, _currentUser.UserId, ct), cancellationToken),
            "delete" => McpErrorMapper.FromCoreResult(await _fridgeService.DeleteAsync(RequireGuid(request.Id, nameof(request.Id)), _currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported fridge action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleFridgeIngredientAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<FridgeIngredientToolArguments>(arguments);
        return request.Action switch
        {
            "create" => await ExecuteCoreValidatedAsync(request.Create, _createFridgeIngredientValidator, async (payload, ct) =>
                await _fridgeIngredientService.CreateAsync(payload, _currentUser.UserId, ct), cancellationToken),
            "list" => McpErrorMapper.FromCoreResult(await _fridgeIngredientService.GetAllAsync(RequireGuid(request.FridgeId, nameof(request.FridgeId)), _currentUser.UserId, cancellationToken)),
            "get" => McpErrorMapper.FromCoreResult(await _fridgeIngredientService.GetByIdAsync(RequireGuid(request.IngredientId, nameof(request.IngredientId)), _currentUser.UserId, cancellationToken)),
            "update" => await ExecuteCoreValidatedAsync(request.Update, _updateFridgeIngredientValidator, async (payload, ct) =>
                await _fridgeIngredientService.UpdateAsync(RequireGuid(request.IngredientId, nameof(request.IngredientId)), payload, _currentUser.UserId, ct), cancellationToken),
            "delete" => McpErrorMapper.FromCoreResult(await _fridgeIngredientService.DeleteAsync(RequireGuid(request.IngredientId, nameof(request.IngredientId)), _currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported fridge ingredient action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleShoppingListAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<ShoppingListToolArguments>(arguments);
        return request.Action switch
        {
            "create" => await ExecuteCoreValidatedAsync(request.Create, _createShoppingListValidator, async (payload, ct) =>
                await _shoppingListService.CreateAsync(payload, _currentUser.UserId, ct), cancellationToken),
            "list" => McpErrorMapper.FromCoreResult(await _shoppingListService.GetAllAsync(_currentUser.UserId, cancellationToken)),
            "update" => await ExecuteCoreValidatedAsync(request.Update, _updateShoppingListValidator, async (payload, ct) =>
                await _shoppingListService.UpdateAsync(RequireGuid(request.Id, nameof(request.Id)), payload, _currentUser.UserId, ct), cancellationToken),
            "delete" => McpErrorMapper.FromCoreResult(await _shoppingListService.DeleteAsync(RequireGuid(request.Id, nameof(request.Id)), _currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported shopping-list action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleShoppingListItemAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        var request = Deserialize<ShoppingListItemToolArguments>(arguments);
        return request.Action switch
        {
            "list" => McpErrorMapper.FromCoreResult(await _shoppingListItemService.GetAllByListIdAsync(request.ListId, _currentUser.UserId, cancellationToken)),
            "add" => await ExecuteCoreValidatedAsync(request.Add, _createShoppingListItemValidator, async (payload, ct) =>
                await _shoppingListItemService.AddAsync(request.ListId, payload, _currentUser.UserId, ct), cancellationToken),
            "bulk-add" => await ExecuteCoreValidatedAsync(request.BulkAdd, _bulkAddShoppingListItemsValidator, async (payload, ct) =>
                await _shoppingListItemService.BulkAddAsync(request.ListId, payload, _currentUser.UserId, ct), cancellationToken),
            "toggle" => McpErrorMapper.FromCoreResult(await _shoppingListItemService.ToggleCheckedAsync(request.ListId, RequireGuid(request.ItemId, nameof(request.ItemId)), _currentUser.UserId, cancellationToken)),
            "delete" => McpErrorMapper.FromCoreResult(await _shoppingListItemService.DeleteAsync(request.ListId, RequireGuid(request.ItemId, nameof(request.ItemId)), _currentUser.UserId, cancellationToken)),
            "delete-checked" => McpErrorMapper.FromCoreResult(await _shoppingListItemService.DeleteCheckedAsync(request.ListId, _currentUser.UserId, cancellationToken)),
            _ => throw new InvalidOperationException($"Unsupported shopping-list item action '{request.Action}'."),
        };
    }

    private async Task<McpToolCallResult> HandleSuggestRecipesAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        return await ExecuteCoreValidatedAsync(Deserialize<SuggestRecipesRequest>(arguments), _suggestRecipesValidator, async (payload, ct) =>
            await _recipeAssistantManager.SuggestRecipesAsync(payload.FridgeId, _currentUser.UserId, ct), cancellationToken);
    }

    private async Task<McpToolCallResult> HandleParseRecipeAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        return await ExecuteCoreValidatedAsync(Deserialize<ParseRecipeRequest>(arguments), _parseRecipeValidator, async (payload, ct) =>
            await _recipeAssistantManager.ParseRecipeAsync(payload.RawText, ct), cancellationToken);
    }

    private async Task<McpToolCallResult> HandleAdaptRecipeAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryEnsureAuthenticated(out var authError))
            return authError!;

        return await ExecuteCoreValidatedAsync(Deserialize<AdaptRecipeRequest>(arguments), _adaptRecipeValidator, async (payload, ct) =>
            await _recipeAssistantManager.AdaptRecipeAsync(payload.RecipeId, payload.Constraints, _currentUser.UserId, ct), cancellationToken);
    }

    private async Task<McpToolCallResult> ExecuteCoreValidatedAsync<TRequest, TResponse>(
        TRequest? request,
        IRequestValidator<TRequest> validator,
        Func<TRequest, CancellationToken, Task<CoreResult.Result<TResponse>>> action,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        if (request is null)
            throw new InvalidOperationException("The request payload is required.");

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return McpErrorMapper.ToolError(validation.Errors);

        return McpErrorMapper.FromCoreResult(await action(request, cancellationToken));
    }

    private async Task<McpToolCallResult> ExecuteIdentityValidatedAsync<TRequest, TResponse>(
        TRequest? request,
        IRequestValidator<TRequest> validator,
        Func<TRequest, CancellationToken, Task<IdentityResult.Result<TResponse>>> action,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        if (request is null)
            throw new InvalidOperationException("The request payload is required.");

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return McpErrorMapper.ToolError(validation.Errors);

        return McpErrorMapper.FromIdentityResult(await action(request, cancellationToken));
    }

    private async Task<McpToolCallResult> ExecuteCoreValidatedAsync<TRequest, TResponse>(
        JsonElement arguments,
        IRequestValidator<TRequest> validator,
        Func<TRequest, CancellationToken, Task<CoreResult.Result<TResponse>>> action,
        CancellationToken cancellationToken)
        where TRequest : class =>
        await ExecuteCoreValidatedAsync(Deserialize<TRequest>(arguments), validator, action, cancellationToken);

    private async Task<McpToolCallResult> ExecuteIdentityValidatedAsync<TRequest, TResponse>(
        JsonElement arguments,
        IRequestValidator<TRequest> validator,
        Func<TRequest, CancellationToken, Task<IdentityResult.Result<TResponse>>> action,
        CancellationToken cancellationToken)
        where TRequest : class =>
        await ExecuteIdentityValidatedAsync(Deserialize<TRequest>(arguments), validator, action, cancellationToken);

    private static T Deserialize<T>(JsonElement arguments) where T : class =>
        arguments.Deserialize<T>(SerializerOptions)
        ?? throw new InvalidOperationException("Unable to deserialize the tool arguments.");

    private static Guid RequireGuid(Guid? id, string propertyName) =>
        id ?? throw new InvalidOperationException($"The '{propertyName}' argument is required.");

    private bool TryEnsureAuthenticated(out McpToolCallResult? error)
    {
        if (_currentUser.IsAuthenticated)
        {
            error = null;
            return true;
        }

        error = McpErrorMapper.ToolError("Authentication is required. Provide a valid Bearer token.", "unauthorized");
        return false;
    }

    private sealed record McpToolRegistration(McpToolDefinition Definition, Func<JsonElement, CancellationToken, Task<McpToolCallResult>> ExecuteAsync);

    private sealed class GuidEnvelope
    {
        public required Guid Id { get; init; }
    }
}
