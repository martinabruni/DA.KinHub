using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kin.KinHub.Core.Business.ChatFeature;

internal sealed class KinHubChatToolExecutor : IChatToolExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IShoppingListService _shoppingListService;
    private readonly IShoppingListItemService _shoppingListItemService;

    public KinHubChatToolExecutor(
        IShoppingListService shoppingListService,
        IShoppingListItemService shoppingListItemService)
    {
        _shoppingListService = shoppingListService;
        _shoppingListItemService = shoppingListItemService;
    }

    public async Task<Result<ChatToolExecutionResult>> ExecuteAsync(
        ChatToolCall toolCall,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return toolCall.ToolName switch
            {
                "list_shopping_lists" => await ListShoppingListsAsync(userId, cancellationToken),
                "create_shopping_list" => await CreateShoppingListAsync(toolCall.ArgumentsJson, userId, cancellationToken),
                "add_shopping_list_item" => await AddShoppingListItemAsync(toolCall.ArgumentsJson, userId, cancellationToken),
                _ => Result<ChatToolExecutionResult>.ValidationError($"Tool '{toolCall.ToolName}' is not supported yet."),
            };
        }
        catch (JsonException ex)
        {
            return Result<ChatToolExecutionResult>.ValidationError($"Invalid tool arguments: {ex.Message}");
        }
    }

    private async Task<Result<ChatToolExecutionResult>> ListShoppingListsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _shoppingListService.GetAllAsync(userId, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return MapFailure(result);

        if (result.Value.Count == 0)
        {
            return Result<ChatToolExecutionResult>.Success(new ChatToolExecutionResult
            {
                MessageContent = "There are no shopping lists yet.",
            });
        }

        var lines = result.Value
            .Select(list => $"- {list.Name} ({list.ItemCount} items, {list.CheckedCount} checked)")
            .ToArray();

        return Result<ChatToolExecutionResult>.Success(new ChatToolExecutionResult
        {
            MessageContent = $"Shopping lists:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}",
        });
    }

    private async Task<Result<ChatToolExecutionResult>> CreateShoppingListAsync(
        string argumentsJson,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var arguments = Deserialize<CreateShoppingListToolArguments>(argumentsJson);
        var listName = arguments.Name.Trim();
        if (string.IsNullOrWhiteSpace(listName))
            return Result<ChatToolExecutionResult>.ValidationError("Shopping list name is required.");

        var createResult = await _shoppingListService.CreateAsync(
            new CreateShoppingListRequest { Name = listName },
            userId,
            cancellationToken);
        if (!createResult.IsSuccess || createResult.Value is null)
            return MapFailure(createResult);

        var itemNames = arguments.Items
            .Select(item => item.Trim())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (itemNames.Length > 0)
        {
            var bulkAddResult = await _shoppingListItemService.BulkAddAsync(
                createResult.Value.Id,
                new BulkAddShoppingListItemsRequest
                {
                    ShoppingListId = createResult.Value.Id,
                    Names = itemNames,
                },
                userId,
                cancellationToken);
            if (!bulkAddResult.IsSuccess || bulkAddResult.Value is null)
                return MapFailure(bulkAddResult);
        }

        var createdItemsLabel = itemNames.Length == 1 ? "item" : "items";
        var summary = itemNames.Length == 0
            ? $"Created shopping list '{createResult.Value.Name}'."
            : $"Created shopping list '{createResult.Value.Name}' with {itemNames.Length} {createdItemsLabel}.";

        return Result<ChatToolExecutionResult>.Success(new ChatToolExecutionResult
        {
            MessageContent = summary,
        });
    }

    private async Task<Result<ChatToolExecutionResult>> AddShoppingListItemAsync(
        string argumentsJson,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var arguments = Deserialize<AddShoppingListItemToolArguments>(argumentsJson);
        if (arguments.ShoppingListId == Guid.Empty)
            return Result<ChatToolExecutionResult>.ValidationError("A shopping_list_id is required.");

        var itemName = arguments.ItemName.Trim();
        if (string.IsNullOrWhiteSpace(itemName))
            return Result<ChatToolExecutionResult>.ValidationError("An item_name is required.");

        var addResult = await _shoppingListItemService.AddAsync(
            arguments.ShoppingListId,
            new CreateShoppingListItemRequest
            {
                ShoppingListId = arguments.ShoppingListId,
                Name = itemName,
            },
            userId,
            cancellationToken);
        if (!addResult.IsSuccess || addResult.Value is null)
            return MapFailure(addResult);

        return Result<ChatToolExecutionResult>.Success(new ChatToolExecutionResult
        {
            MessageContent = $"Added '{addResult.Value.Name}' to the shopping list.",
        });
    }

    private static TArguments Deserialize<TArguments>(string argumentsJson)
    {
        var value = JsonSerializer.Deserialize<TArguments>(argumentsJson, JsonOptions);
        return value ?? throw new JsonException("Tool arguments payload is empty.");
    }

    private static Result<ChatToolExecutionResult> MapFailure<T>(Result<T> result) =>
        result.Status switch
        {
            ResultStatus.NotFound => Result<ChatToolExecutionResult>.NotFound(result.Message ?? "Resource not found."),
            ResultStatus.Conflict => Result<ChatToolExecutionResult>.Conflict(result.Message ?? "Conflict while executing tool."),
            ResultStatus.ValidationError => Result<ChatToolExecutionResult>.ValidationError(result.Message ?? "Tool validation failed."),
            ResultStatus.Unauthorized => Result<ChatToolExecutionResult>.Unauthorized(result.Message ?? "Access denied."),
            _ => Result<ChatToolExecutionResult>.UnexpectedError(result.Message ?? "Unexpected tool execution error."),
        };

    private sealed class CreateShoppingListToolArguments
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("items")]
        public IReadOnlyList<string> Items { get; init; } = [];
    }

    private sealed class AddShoppingListItemToolArguments
    {
        [JsonPropertyName("shopping_list_id")]
        public Guid ShoppingListId { get; init; }

        [JsonPropertyName("item_name")]
        public string ItemName { get; init; } = string.Empty;
    }
}
