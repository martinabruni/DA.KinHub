using Kin.KinHub.KinList.Business.Common;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IKinListAudioDraftGenerator
{
    Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default);
}
