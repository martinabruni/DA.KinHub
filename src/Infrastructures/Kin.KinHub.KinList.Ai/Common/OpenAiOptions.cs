namespace Kin.KinHub.KinList.Ai.Common;

public sealed class OpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ModelDeploymentName { get; set; } = "gpt-4o-mini";

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(ApiKey);

    public bool HasPartialConfiguration() =>
        !string.IsNullOrWhiteSpace(Endpoint)
        || !string.IsNullOrWhiteSpace(ApiKey);

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new InvalidOperationException($"{nameof(OpenAiOptions)}.{nameof(Endpoint)} is required.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException($"{nameof(OpenAiOptions)}.{nameof(ApiKey)} is required.");
        }

        if (string.IsNullOrWhiteSpace(ModelDeploymentName))
        {
            throw new InvalidOperationException($"{nameof(OpenAiOptions)}.{nameof(ModelDeploymentName)} is required.");
        }
    }
}
