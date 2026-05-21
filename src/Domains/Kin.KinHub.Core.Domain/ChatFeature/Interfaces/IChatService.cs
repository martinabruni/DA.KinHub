namespace Kin.KinHub.Core.Domain.ChatFeature;

public interface IChatService
{
    /// <summary>
    /// Sends the conversation history to the AI model and returns the assistant response.
    /// If the model wants to call a tool, returns the tool call details instead of a text response.
    /// </summary>
    Task<ChatServiceResponse> SendAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default);
}
