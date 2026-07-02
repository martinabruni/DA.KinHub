namespace Kin.KinHub.KinList.Ai.KinListFeature;

public interface IKinListChatCompletionClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
