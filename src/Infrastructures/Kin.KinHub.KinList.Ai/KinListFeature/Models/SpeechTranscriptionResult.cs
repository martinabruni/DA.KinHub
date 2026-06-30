namespace Kin.KinHub.KinList.Ai.KinListFeature;

public sealed class SpeechTranscriptionResult
{
    public required string Transcript { get; set; }
    public required string DetectedLanguage { get; set; }
}
