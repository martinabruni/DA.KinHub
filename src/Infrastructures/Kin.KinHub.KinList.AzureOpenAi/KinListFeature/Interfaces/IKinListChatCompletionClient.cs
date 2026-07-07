namespace Kin.KinHub.KinList.AzureOpenAi.KinListFeature;

public interface IKinListChatCompletionClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
