using Azure;
using Azure.AI.OpenAI;
using Kin.KinHub.Core.OpenAi.Common;
using OpenAI.Embeddings;

namespace Kin.KinHub.Core.OpenAi.RecipeAssistantFeature;

internal sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly OpenAiOptions _options;

    public OpenAiEmbeddingService(OpenAiOptions options)
    {
        _options = options;
        var client = new AzureOpenAIClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
        _embeddingClient = client.GetEmbeddingClient(options.EmbeddingDeploymentName);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await OpenAiExecutionHelper.ExecuteWithResilienceAsync(
            ct => _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: ct),
            "openai.embedding.generate",
            _options,
            cancellationToken);
        return result.Value.ToFloats().ToArray();
    }
}
