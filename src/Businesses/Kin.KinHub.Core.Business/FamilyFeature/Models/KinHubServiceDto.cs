namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class KinHubServiceDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string BaseUrl { get; init; }
    public required bool IsActive { get; init; }
}
