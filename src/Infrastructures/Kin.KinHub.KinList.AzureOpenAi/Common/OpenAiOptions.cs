namespace Kin.KinHub.KinList.AzureOpenAi.Common;

public sealed class OpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ModelDeploymentName { get; set; } = "gpt-4o-mini";
    public bool UseManagedIdentity { get; set; }

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && (UseManagedIdentity || !string.IsNullOrWhiteSpace(ApiKey));

    public bool HasPartialConfiguration() =>
        !string.IsNullOrWhiteSpace(Endpoint)
        || !string.IsNullOrWhiteSpace(ApiKey)
        || UseManagedIdentity;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new InvalidOperationException($"{nameof(OpenAiOptions)}.{nameof(Endpoint)} is required.");
        }

        if (string.IsNullOrWhiteSpace(ModelDeploymentName))
        {
            throw new InvalidOperationException($"{nameof(OpenAiOptions)}.{nameof(ModelDeploymentName)} is required.");
        }
    }
}
