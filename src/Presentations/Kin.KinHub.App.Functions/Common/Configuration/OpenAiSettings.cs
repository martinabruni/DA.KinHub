namespace Kin.KinHub.App.Functions.Common.Configuration;

public sealed class OpenAiSettings
{
    public const string SectionName = "OpenAi";
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";
    public string ModelDeploymentName { get; set; } = "gpt-4o-mini";
}
