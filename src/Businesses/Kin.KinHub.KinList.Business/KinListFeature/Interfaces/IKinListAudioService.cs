using Kin.KinHub.KinList.Business.Common;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IKinListAudioService
{
    Task<Result<CreateAudioProcessingOperationResponse>> CreateAudioOperationAsync(CreateAudioProcessingOperationRequest request, Guid familyId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<AudioProcessingOperationResponse>> CompleteAudioOperationUploadAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default);
    Task<Result<AudioProcessingOperationResponse>> GetAudioOperationAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAudioOperationAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default);
    Task<Result<AudioProcessingOperationResponse>> ProcessAudioOperationAsync(Guid operationId, CancellationToken cancellationToken = default);
    Task<Result<AudioProcessingOperationResponse>> MarkAudioOperationFailedAsync(Guid operationId, string code, string message, CancellationToken cancellationToken = default);
    Task<Result<KinListDraftFromAudioResponse>> CreateDraftFromAudioAsync(KinListAudioCommand command, CancellationToken cancellationToken = default);
    Task<Result<KinListItemDraftsFromAudioResponse>> CreateItemDraftsFromAudioAsync(Guid listId, Guid familyId, KinListAudioCommand command, CancellationToken cancellationToken = default);
}
