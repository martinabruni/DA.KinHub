namespace Kin.KinHub.KinList.Ai.KinListFeature;

public interface IKinListAudioPromptInterpreter
{
    Task<Result<ParsedKinListAudioDraft>> InterpretAsync(SpeechTranscriptionResult transcription, CancellationToken cancellationToken = default);
}
