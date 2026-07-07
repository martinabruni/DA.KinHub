using Azure;
using Azure.AI.OpenAI;
using Kin.KinHub.KinRecipe.AzureOpenAi.Common;
using Mapster;
using OpenAI.Chat;
using System.Text.Json;

namespace Kin.KinHub.KinRecipe.AzureOpenAi.RecipeAssistantFeature;

internal sealed class OpenAiRecipeAssistantService : IRecipeAssistantService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ChatClient _chatClient;
    private readonly OpenAiOptions _options;
    private readonly string _parsePrompt;
    private readonly string _suggestPrompt;
    private readonly string _adaptPrompt;

    public OpenAiRecipeAssistantService(OpenAiOptions options)
    {
        _options = options;
        var client = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
        _chatClient = client.GetChatClient(options.ModelDeploymentName);
        _parsePrompt = options.ParseRecipeSystemPrompt;
        _suggestPrompt = options.SuggestRecipesSystemPrompt;
        _adaptPrompt = options.AdaptRecipeSystemPrompt;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RecipeSuggestion>> SuggestNewRecipesAsync(
        IReadOnlyList<RecipeIngredient> fridgeIngredients,
        CancellationToken cancellationToken = default)
    {
        var input = new
        {
            task_type = "recipe_suggestion",
            fridge_ingredients = fridgeIngredients.Select(i => new { i.Name, i.Quantity, unit = i.MeasureUnit }),
        };

        var json = await SendAsync(JsonSerializer.Serialize(input, JsonOptions), temperature: 0.7f, _suggestPrompt, cancellationToken);
        var response = DeserializeRequired<SuggestionResponse>(json, "recipe suggestion response");
        if (!string.Equals(response.TaskType, "recipe_suggestion", StringComparison.Ordinal))
        {
            throw OpenAiExecutionHelper.InvalidResponse("Azure OpenAI returned an unexpected task_type for recipe suggestions.", json);
        }

        return response.Suggestions.Adapt<List<RecipeSuggestion>>();
    }

    /// <inheritdoc/>
    public async Task<Recipe?> ParseRecipeAsync(
        string rawText,
        CancellationToken cancellationToken = default)
    {
        var input = new { task_type = "recipe_parsing", raw_text = rawText };

        var json = await SendAsync(JsonSerializer.Serialize(input, JsonOptions), temperature: 0.3f, _parsePrompt, cancellationToken);
        var response = DeserializeRequired<ParseResponse>(json, "recipe parsing response");
        if (!string.Equals(response.TaskType, "recipe_parsing", StringComparison.Ordinal))
        {
            throw OpenAiExecutionHelper.InvalidResponse("Azure OpenAI returned an unexpected task_type for recipe parsing.", json);
        }

        return response.Recipe?.Adapt<Recipe>();
    }

    /// <inheritdoc/>
    public async Task<RecipeAdaptationResult> AdaptRecipeAsync(
        Recipe recipe,
        IReadOnlyList<string> constraints,
        CancellationToken cancellationToken = default)
    {
        var input = new
        {
            task_type = "recipe_adaptation",
            recipe = MapToJsonObject(recipe),
            constraints,
        };

        var json = await SendAsync(JsonSerializer.Serialize(input, JsonOptions), temperature: 0.3f, _adaptPrompt, cancellationToken);
        var response = DeserializeRequired<AdaptationResponse>(json, "recipe adaptation response");
        if (!string.Equals(response.TaskType, "recipe_adaptation", StringComparison.Ordinal))
        {
            throw OpenAiExecutionHelper.InvalidResponse("Azure OpenAI returned an unexpected task_type for recipe adaptation.", json);
        }

        var originalRecipe = response.OriginalRecipe.Adapt<Recipe>();
        var changedIds = new List<Guid>();
        var adaptedIngredients = originalRecipe.Ingredients?.ToList() ?? [];

        foreach (var change in response.Changes)
        {
            if (change.OriginalIngredientId is not null
                && Guid.TryParse(change.OriginalIngredientId, out var changedId))
            {
                changedIds.Add(changedId);

                var idx = adaptedIngredients.FindIndex(i => i.Id == changedId);
                if (idx >= 0)
                {
                    if (change.NewIngredient is not null)
                        adaptedIngredients[idx] = change.NewIngredient.Adapt<RecipeIngredient>();
                    else
                        adaptedIngredients.RemoveAt(idx);
                }
            }
            else if (change.NewIngredient is not null && change.Type == "addition")
            {
                adaptedIngredients.Add(change.NewIngredient.Adapt<RecipeIngredient>());
            }
        }

        var adaptedRecipe = new Recipe
        {
            Id = Guid.Empty,
            Name = originalRecipe.Name,
            Backstory = originalRecipe.Backstory,
            FinalTime = originalRecipe.FinalTime,
            Portions = originalRecipe.Portions,
            RecipeBookId = Guid.Empty,
            Ingredients = adaptedIngredients,
            Steps = response.AdaptedSteps.Adapt<List<RecipeStep>>().OrderBy(s => s.Order).ToList(),
        };

        return new RecipeAdaptationResult
        {
            OriginalRecipe = originalRecipe,
            AdaptedRecipe = adaptedRecipe,
            Changes = response.Changes.Adapt<List<RecipeChange>>(),
            ChangedOriginalIngredientIds = changedIds,
        };
    }

    private async Task<string> SendAsync(string userMessage, float temperature, string systemPrompt, CancellationToken cancellationToken)
    {
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            Temperature = temperature,
        };

        var result = await OpenAiExecutionHelper.ExecuteWithResilienceAsync(
            ct => _chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userMessage),
                ],
                options,
                ct),
            "openai.chat.complete",
            _options,
            cancellationToken);

        if (result.Value.Content.Count is 0 || string.IsNullOrWhiteSpace(result.Value.Content[0].Text))
        {
            throw OpenAiExecutionHelper.InvalidResponse("Azure OpenAI returned an empty JSON payload.");
        }

        return result.Value.Content[0].Text;
    }

    private static T DeserializeRequired<T>(string json, string context)
    {
        try
        {
            var response = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (response is null)
            {
                throw OpenAiExecutionHelper.InvalidResponse($"Azure OpenAI returned an empty {context}.", json);
            }

            return response;
        }
        catch (JsonException ex)
        {
            throw OpenAiExecutionHelper.InvalidResponse($"Azure OpenAI returned an invalid {context}.", json, ex);
        }
    }

    private static object MapToJsonObject(Recipe recipe) =>
        new
        {
            recipe.Name,
            recipe.Backstory,
            recipe.FinalTime,
            recipe.Portions,
            Ingredients = recipe.Ingredients?.Select(i => new { id = i.Id, i.Name, i.Quantity, unit = i.MeasureUnit }) ?? [],
            Steps = recipe.Steps?.Select(s => new { s.Order, s.Description }) ?? [],
        };

}
