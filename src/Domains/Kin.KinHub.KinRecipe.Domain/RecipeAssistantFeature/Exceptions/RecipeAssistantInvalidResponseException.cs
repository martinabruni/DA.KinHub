namespace Kin.KinHub.KinRecipe.Domain.RecipeAssistantFeature;

public sealed class RecipeAssistantInvalidResponseException : RecipeAssistantIntegrationException
{
    public RecipeAssistantInvalidResponseException(string message, string? payload = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Payload = payload;
    }

    public string? Payload { get; }
}
