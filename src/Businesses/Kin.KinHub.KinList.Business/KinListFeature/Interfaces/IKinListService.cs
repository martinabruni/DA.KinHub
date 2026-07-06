using Kin.KinHub.KinList.Business.Common;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public interface IKinListService
{
    Task<Result<IReadOnlyList<KinListResponse>>> GetAllAsync(Guid familyId, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> GetByIdAsync(Guid listId, Guid familyId, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> CreateAsync(CreateKinListRequest request, Guid familyId, Guid userId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> UpdateAsync(Guid listId, UpdateKinListRequest request, Guid familyId, string ifMatch, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> DeleteAsync(Guid listId, Guid familyId, string ifMatch, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> RestoreAsync(Guid listId, Guid familyId, string ifMatch, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> AddItemAsync(Guid listId, CreateKinListItemRequest request, Guid familyId, string ifMatch, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> BulkConfirmItemsAsync(Guid listId, BulkConfirmKinListItemsRequest request, Guid familyId, string ifMatch, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> UpdateItemAsync(Guid listId, Guid itemId, UpdateKinListItemRequest request, Guid familyId, string ifMatch, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> DeleteItemAsync(Guid listId, Guid itemId, Guid familyId, string ifMatch, CancellationToken cancellationToken = default);
    Task<Result<KinListDetailResponse>> RestoreItemAsync(Guid listId, Guid itemId, Guid familyId, string ifMatch, CancellationToken cancellationToken = default);
    Task<Result<CreateAudioProcessingOperationResponse>> CreateAudioOperationAsync(CreateAudioProcessingOperationRequest request, Guid familyId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<AudioProcessingOperationResponse>> CompleteAudioOperationUploadAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default);
    Task<Result<AudioProcessingOperationResponse>> GetAudioOperationAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAudioOperationAsync(Guid operationId, Guid familyId, CancellationToken cancellationToken = default);
    Task<Result<AudioProcessingOperationResponse>> ProcessAudioOperationAsync(Guid operationId, CancellationToken cancellationToken = default);
    Task<Result<AudioProcessingOperationResponse>> MarkAudioOperationFailedAsync(Guid operationId, string code, string message, CancellationToken cancellationToken = default);
}
