namespace Kin.KinHub.Core.Api.Common.Authorization;

public interface IFamilyContextResolver
{
    Task<FamilyContextResolution> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
}
