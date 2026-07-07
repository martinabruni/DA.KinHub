namespace Kin.KinHub.KinList.AzureOpenAi.KinListFeature;

public interface IKinListSpeechTranscriber
{
    Task<Result<SpeechTranscriptionResult>> TranscribeAsync(KinListAudioCommand command, CancellationToken cancellationToken = default);
}
