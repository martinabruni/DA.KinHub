using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class KinHubFamilyService : IFamilyService
{
    private readonly ICreateFamilyHandler _createFamilyHandler;
    private readonly IAddFamilyMemberHandler _addFamilyMemberHandler;
    private readonly IGetFamilyHandler _getFamilyHandler;
    private readonly IDeleteFamilyMemberHandler _deleteFamilyMemberHandler;
    private readonly IUpdateFamilyMemberHandler _updateFamilyMemberHandler;
    private readonly IUpdateFamilyHandler _updateFamilyHandler;
    private readonly IDeleteFamilyHandler _deleteFamilyHandler;

    public KinHubFamilyService(
        ICreateFamilyHandler createFamilyHandler,
        IAddFamilyMemberHandler addFamilyMemberHandler,
        IGetFamilyHandler getFamilyHandler,
        IDeleteFamilyMemberHandler deleteFamilyMemberHandler,
        IUpdateFamilyMemberHandler updateFamilyMemberHandler,
        IUpdateFamilyHandler updateFamilyHandler,
        IDeleteFamilyHandler deleteFamilyHandler)
    {
        _createFamilyHandler = createFamilyHandler;
        _addFamilyMemberHandler = addFamilyMemberHandler;
        _getFamilyHandler = getFamilyHandler;
        _deleteFamilyMemberHandler = deleteFamilyMemberHandler;
        _updateFamilyMemberHandler = updateFamilyMemberHandler;
        _updateFamilyHandler = updateFamilyHandler;
        _deleteFamilyHandler = deleteFamilyHandler;
    }

    /// <inheritdoc/>
    public Task<Result<CreateFamilyResponse>> CreateFamilyAsync(
        CreateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _createFamilyHandler.HandleAsync(request, userId, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<AddFamilyMemberResponse>> AddFamilyMemberAsync(
        Guid familyId,
        AddFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _addFamilyMemberHandler.HandleAsync(familyId, request, userId, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<FamilyDetailResponse>> GetFamilyAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _getFamilyHandler.HandleAsync(userId, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<bool>> DeleteFamilyMemberAsync(
        Guid familyId,
        Guid memberId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _deleteFamilyMemberHandler.HandleAsync(familyId, memberId, userId, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<UpdateFamilyMemberResponse>> UpdateFamilyMemberAsync(
        Guid familyId,
        Guid memberId,
        UpdateFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _updateFamilyMemberHandler.HandleAsync(familyId, memberId, request, userId, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<UpdateFamilyResponse>> UpdateFamilyAsync(
        Guid familyId,
        UpdateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _updateFamilyHandler.HandleAsync(familyId, request, userId, cancellationToken);

    /// <inheritdoc/>
    public Task<Result<bool>> DeleteFamilyAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _deleteFamilyHandler.HandleAsync(familyId, userId, cancellationToken);
}
