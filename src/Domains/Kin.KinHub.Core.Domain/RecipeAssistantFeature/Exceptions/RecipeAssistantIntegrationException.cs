namespace Kin.KinHub.Core.Domain.RecipeAssistantFeature;

public abstract class RecipeAssistantIntegrationException : Exception
{
    protected RecipeAssistantIntegrationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class RecipeAssistantUnavailableException : RecipeAssistantIntegrationException
{
    public RecipeAssistantUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class RecipeAssistantInvalidResponseException : RecipeAssistantIntegrationException
{
    public RecipeAssistantInvalidResponseException(string message, string? payload = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Payload = payload;
    }

    public string? Payload { get; }
}
