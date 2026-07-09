namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IUpdateFamilyHandler
{
    Task<Result<UpdateFamilyResponse>> HandleAsync(
        Guid familyId,
        UpdateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
