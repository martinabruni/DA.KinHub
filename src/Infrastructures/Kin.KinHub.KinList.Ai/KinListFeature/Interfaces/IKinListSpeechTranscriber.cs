namespace Kin.KinHub.KinList.Ai.KinListFeature;

public interface IKinListSpeechTranscriber
{
    Task<Result<SpeechTranscriptionResult>> TranscribeAsync(KinListAudioCommand command, CancellationToken cancellationToken = default);
}
