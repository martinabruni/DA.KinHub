namespace Kin.KinHub.KinList.AzureOpenAi.KinListFeature;

public interface IKinListAudioPromptInterpreter
{
    Task<Result<ParsedKinListAudioDraft>> InterpretAsync(SpeechTranscriptionResult transcription, CancellationToken cancellationToken = default);
}
