namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IDeleteFamilyHandler
{
    Task<Result<bool>> HandleAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
