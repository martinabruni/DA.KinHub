namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class FamilyDetailResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<FamilyMemberDto> Members { get; init; }
}
