namespace Kin.KinHub.KinRecipe.Domain.RecipeAssistantFeature;

/// <summary>
/// Service for generating embedding vectors from text.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Generates a float[] embedding for the given text.</summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
