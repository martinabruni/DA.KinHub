namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IUserProviderService
{
    Task<Result<IReadOnlyList<LinkedProviderResponse>>> GetProvidersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LinkedProviderResponse>>> LinkAsync(
        Guid userId,
        LinkProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LinkedProviderResponse>>> UnlinkAsync(
        Guid userId,
        IdentityProviderType provider,
        CancellationToken cancellationToken = default);
}
