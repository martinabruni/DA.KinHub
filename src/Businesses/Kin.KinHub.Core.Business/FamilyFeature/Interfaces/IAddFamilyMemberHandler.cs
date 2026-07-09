namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IAddFamilyMemberHandler
{
    Task<Result<AddFamilyMemberResponse>> HandleAsync(
        Guid familyId,
        AddFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}
