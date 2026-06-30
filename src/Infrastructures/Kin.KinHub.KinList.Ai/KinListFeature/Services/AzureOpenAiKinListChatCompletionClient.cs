using Azure;
using Azure.AI.OpenAI;
using Kin.KinHub.KinList.Ai.Common;
using OpenAI.Chat;

namespace Kin.KinHub.KinList.Ai.KinListFeature;

internal sealed class AzureOpenAiKinListChatCompletionClient : IKinListChatCompletionClient
{
    private readonly ChatClient _chatClient;
    private readonly KinListOptions _kinListOptions;

    public AzureOpenAiKinListChatCompletionClient(OpenAiOptions options, KinListOptions kinListOptions)
    {
        var client = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
        _chatClient = client.GetChatClient(options.ModelDeploymentName);
        _kinListOptions = kinListOptions;
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
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
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
}
