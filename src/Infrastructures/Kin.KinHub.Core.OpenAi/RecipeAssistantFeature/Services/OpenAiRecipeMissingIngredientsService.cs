
namespace Kin.KinHub.Core.OpenAi.RecipeAssistantFeature;

internal sealed class OpenAiRecipeMissingIngredientsService : IRecipeMissingIngredientsService
{
    private const float SimilarityThreshold = 0.85f;

    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IFridgeIngredientRepository _fridgeIngredientRepository;

    public OpenAiRecipeMissingIngredientsService(
        IRecipeIngredientRepository recipeIngredientRepository,
        IFridgeIngredientRepository fridgeIngredientRepository)
    {
        _recipeIngredientRepository = recipeIngredientRepository;
        _fridgeIngredientRepository = fridgeIngredientRepository;
    }

    public async Task<IReadOnlyList<string>> GetMissingIngredientsAsync(
        Guid recipeId,
        Guid fridgeId,
        CancellationToken cancellationToken = default)
    {
        var recipeIngredients = await _recipeIngredientRepository.GetAllByRecipeIdAsync(recipeId, cancellationToken);
        var fridgeIngredients = await _fridgeIngredientRepository.GetAllByFridgeIdAsync(fridgeId, cancellationToken);

        var missing = new List<string>();

        foreach (var recipeIngredient in recipeIngredients)
        {
            if (recipeIngredient.Embedding is null)
            {
                var fallbackFound = fridgeIngredients.Any(fi =>
                    string.Equals(NormalizeIngredientName(fi.Name), NormalizeIngredientName(recipeIngredient.Name), StringComparison.OrdinalIgnoreCase));
                if (!fallbackFound)
                {
                    missing.Add(recipeIngredient.Name);
                }
                continue;
            }

            var found = fridgeIngredients.Any(fi =>
                (fi.Embedding is not null &&
                 CosineSimilarity(recipeIngredient.Embedding, fi.Embedding) >= SimilarityThreshold)
                || string.Equals(NormalizeIngredientName(fi.Name), NormalizeIngredientName(recipeIngredient.Name), StringComparison.OrdinalIgnoreCase));

            if (!found)
                missing.Add(recipeIngredient.Name);
        }

        return missing;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        var dot = 0f;
        var magA = 0f;
        var magB = 0f;

        for (var i = 0; i < a.Length && i < b.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        return magA is 0f || magB is 0f ? 0f : dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }

    private static string NormalizeIngredientName(string value) => value.Trim();
}
