namespace Kin.KinHub.Core.Domain.RecipeAssistantFeature;

public sealed class RecipeAssistantUnavailableException : RecipeAssistantIntegrationException
{
    public RecipeAssistantUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
