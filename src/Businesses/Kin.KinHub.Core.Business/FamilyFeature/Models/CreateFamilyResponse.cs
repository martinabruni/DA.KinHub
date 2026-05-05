namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class CreateFamilyResponse
{
    public required Guid FamilyId { get; init; }
    public required Guid OwnerMemberId { get; init; }
}
