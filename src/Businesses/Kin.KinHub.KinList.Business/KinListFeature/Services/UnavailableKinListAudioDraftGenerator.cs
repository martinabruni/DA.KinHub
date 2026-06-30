using Kin.KinHub.KinList.Business.Common;

namespace Kin.KinHub.KinList.Business.KinListFeature;

internal sealed class UnavailableKinListAudioDraftGenerator : IKinListAudioDraftGenerator
{
    public Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<ParsedKinListAudioDraft>.ServiceUnavailable(
            "Audio draft processing is not available in this environment.",
            "audio_processing_unavailable"));
}
