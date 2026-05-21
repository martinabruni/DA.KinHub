using Azure;
using Azure.AI.OpenAI;
using Kin.KinHub.Core.Domain.ChatFeature;
using Kin.KinHub.Core.OpenAi.Common;
using OpenAI.Chat;

namespace Kin.KinHub.Core.OpenAi.ChatFeature;

internal sealed class OpenAiChatService : IChatService
{
    private const string SystemPrompt = """
        You are KinBot, a helpful assistant for the KinHub family app.
        You help users manage their family data: shopping lists, recipes, recipe books, and fridge contents.
        Always respond in the same language the user writes in.
        When you need to perform an action on behalf of the user, use the available tools.
        Always confirm with the user before calling a tool — describe what you are about to do.
        """;

    private static readonly IReadOnlyList<ChatTool> Tools =
    [
        ChatTool.CreateFunctionTool(
            functionName: "list_recipe_books",
            functionDescription: "Lists all recipe books available for the family. Call this before creating a recipe to know where to save it.",
            functionParameters: BinaryData.FromString("""{"type":"object","properties":{}}""")),

        ChatTool.CreateFunctionTool(
            functionName: "list_shopping_lists",
            functionDescription: "Lists all existing shopping lists for the family.",
            functionParameters: BinaryData.FromString("""{"type":"object","properties":{}}""")),

        ChatTool.CreateFunctionTool(
            functionName: "create_shopping_list",
            functionDescription: "Creates a new shopping list with the given name and items.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "name": { "type": "string", "description": "Name of the shopping list" },
                    "items": { "type": "array", "items": { "type": "string" }, "description": "List of item names" }
                  },
                  "required": ["name", "items"]
                }
                """)),

        ChatTool.CreateFunctionTool(
            functionName: "add_shopping_list_item",
            functionDescription: "Adds a new item to an existing shopping list.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "shopping_list_id": { "type": "string", "description": "The ID of the shopping list" },
                    "item_name": { "type": "string", "description": "Name of the item to add" }
                  },
                  "required": ["shopping_list_id", "item_name"]
                }
                """)),

        ChatTool.CreateFunctionTool(
            functionName: "create_recipe",
            functionDescription: "Creates a new recipe in a specific recipe book.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "recipe_book_id": { "type": "string", "description": "The ID of the recipe book" },
                    "name": { "type": "string", "description": "Recipe name" },
                    "ingredients": {
                      "type": "array",
                      "description": "List of ingredients",
                      "items": {
                        "type": "object",
                        "properties": {
                          "name": { "type": "string" },
                          "quantity": { "type": "number" },
                          "unit": { "type": "string" }
                        },
                        "required": ["name", "quantity", "unit"]
                      }
                    },
                    "steps": {
                      "type": "array",
                      "items": { "type": "string" },
                      "description": "Ordered list of preparation steps"
                    }
                  },
                  "required": ["recipe_book_id", "name", "ingredients", "steps"]
                }
                """)),

        ChatTool.CreateFunctionTool(
            functionName: "add_fridge_ingredient",
            functionDescription: "Adds or updates an ingredient in the family fridge.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "fridge_id": { "type": "string", "description": "The ID of the fridge" },
                    "name": { "type": "string", "description": "Ingredient name" },
                    "quantity": { "type": "number", "description": "Quantity" },
                    "unit": { "type": "string", "description": "Unit of measure" }
                  },
                  "required": ["fridge_id", "name", "quantity", "unit"]
                }
                """)),
    ];

    private readonly ChatClient _chatClient;

    public OpenAiChatService(OpenAiOptions options)
    {
        var client = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
        _chatClient = client.GetChatClient(options.ChatDeploymentName);
    }

    /// <inheritdoc/>
    public async Task<ChatServiceResponse> SendAsync(
        IReadOnlyList<Domain.ChatFeature.ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(history);

        var completionOptions = new ChatCompletionOptions();
        foreach (var tool in Tools)
            completionOptions.Tools.Add(tool);

        var result = await _chatClient.CompleteChatAsync(
            messages,
            completionOptions,
            cancellationToken);

        var completion = result.Value;

        if (completion.FinishReason == ChatFinishReason.ToolCalls && completion.ToolCalls.Count > 0)
        {
            var toolCall = completion.ToolCalls[0];
            return new ChatServiceResponse
            {
                ToolCallRequest = new ChatToolCallRequest
                {
                    ToolName = toolCall.FunctionName,
                    ArgumentsJson = toolCall.FunctionArguments.ToString(),
                },
            };
        }

        return new ChatServiceResponse
        {
            TextContent = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty,
        };
    }

    private static List<OpenAI.Chat.ChatMessage> BuildMessages(IReadOnlyList<Domain.ChatFeature.ChatMessage> history)
    {
        List<OpenAI.Chat.ChatMessage> messages = [new SystemChatMessage(SystemPrompt)];

        foreach (var message in history)
        {
            OpenAI.Chat.ChatMessage? mapped = message.Role switch
            {
                Domain.ChatFeature.ChatMessageRole.User => new UserChatMessage(message.Content),
                Domain.ChatFeature.ChatMessageRole.Assistant => new AssistantChatMessage(message.Content),
                Domain.ChatFeature.ChatMessageRole.Tool => null,
                _ => null,
            };

            if (mapped is not null)
                messages.Add(mapped);
        }

        return messages;
    }
}
