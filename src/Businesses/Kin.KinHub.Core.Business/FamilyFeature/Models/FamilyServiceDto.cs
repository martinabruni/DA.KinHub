namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class FamilyServiceDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required bool IsEnabled { get; init; }
}
