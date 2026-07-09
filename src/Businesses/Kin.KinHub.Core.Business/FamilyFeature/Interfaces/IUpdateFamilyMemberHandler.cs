namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IUpdateFamilyMemberHandler
{
    Task<Result<UpdateFamilyMemberResponse>> HandleAsync(
        Guid familyId,
        Guid memberId,
        UpdateFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
