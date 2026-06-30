namespace Kin.KinHub.KinList.Ai.Common;

public sealed class SpeechToTextOptions
{
    public const string SectionName = "Speech";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string[] CandidateLocales { get; set; } =
    [
        "it-IT",
        "en-US",
        "en-GB",
        "fr-FR",
        "de-DE",
        "es-ES",
    ];

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
            throw new InvalidOperationException($"{nameof(SpeechToTextOptions)}.{nameof(Endpoint)} is required.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException($"{nameof(SpeechToTextOptions)}.{nameof(ApiKey)} is required.");
        }

        if (CandidateLocales.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"{nameof(SpeechToTextOptions)}.{nameof(CandidateLocales)} cannot contain blank values.");
        }
    }
}
