namespace Kin.KinHub.App.Functions.Common.Authorization;

public interface IFamilyContextResolver
{
    Task<FamilyContextResolution> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
}
