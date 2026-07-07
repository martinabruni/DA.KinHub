namespace Kin.KinHub.KinRecipe.Domain.RecipeAssistantFeature;

public abstract class RecipeAssistantIntegrationException : Exception
{
    protected RecipeAssistantIntegrationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
