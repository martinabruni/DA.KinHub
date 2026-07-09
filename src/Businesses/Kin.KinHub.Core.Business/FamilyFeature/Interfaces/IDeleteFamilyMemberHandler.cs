namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IDeleteFamilyMemberHandler
{
    Task<Result<bool>> HandleAsync(
        Guid familyId,
        Guid memberId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
