using Azure;
using Azure.AI.OpenAI;
using System.Text;
using Kin.KinHub.KinList.AzureOpenAi.Common;
using OpenAI.Chat;
using System.ClientModel;

namespace Kin.KinHub.KinList.AzureOpenAi.KinListFeature;

internal sealed class AzureOpenAiKinListChatCompletionClient : IKinListChatCompletionClient
{
    private readonly ChatClient _chatClient;
    private readonly KinListOptions _kinListOptions;
    private readonly ChatResponseFormat _responseFormat;

    public AzureOpenAiKinListChatCompletionClient(OpenAiOptions options, KinListOptions kinListOptions)
    {
        var client = options.UseManagedIdentity
            ? new AzureOpenAIClient(new Uri(options.Endpoint), new global::Azure.Identity.DefaultAzureCredential())
            : new AzureOpenAIClient(new Uri(options.Endpoint), new ApiKeyCredential(options.ApiKey));
        _chatClient = client.GetChatClient(options.ModelDeploymentName);
        _kinListOptions = kinListOptions;
        _responseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "kin_list_audio_draft",
            jsonSchema: BuildResponseSchema(kinListOptions),
            jsonSchemaFormatDescription: "Structured KinHub shopping-list draft output for audio transcription.",
            jsonSchemaIsStrict: true);
    }

    public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default) =>
        ExecuteWithTimeoutAsync(
            ct => CompleteCoreAsync(systemPrompt, userMessage, ct),
            cancellationToken);

    private Task<string> CompleteCoreAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken) =>
        TransientExecutionHelper.ExecuteAsync(async ct =>
        {
            var options = new ChatCompletionOptions
            {
                ResponseFormat = _responseFormat,
                Temperature = 0.1f,
            };

            var result = await _chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userMessage),
                ],
                options,
                ct);

            return result.Value.Content[0].Text;
        }, _kinListOptions.TransientRetryMaxAttempts, _kinListOptions.TransientRetryBaseDelayMilliseconds, _kinListOptions.TransientRetryMaxDelayMilliseconds, cancellationToken);

    private async Task<string> ExecuteWithTimeoutAsync(Func<CancellationToken, Task<string>> operation, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_kinListOptions.AudioProcessingTimeoutSeconds));

        try
        {
            return await operation(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Audio structuring timed out.");
        }
    }

    private static BinaryData BuildResponseSchema(KinListOptions options)
    {
        var schema = $$"""
            {
              "type": "object",
              "properties": {
                "title": {
                  "type": "string",
                  "maxLength": {{options.MaxTitleLength}}
                },
                "items": {
                  "type": "array",
                  "maxItems": {{options.MaxItemsPerBulkConfirm}},
                  "items": {
                    "type": "string",
                    "maxLength": {{options.MaxItemLength}}
                  }
                }
              },
              "required": ["title", "items"],
              "additionalProperties": false
            }
            """;
        return BinaryData.FromBytes(Encoding.UTF8.GetBytes(schema));
    }
}
