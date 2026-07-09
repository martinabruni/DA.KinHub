namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IGetFamilyHandler
{
    Task<Result<FamilyDetailResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
