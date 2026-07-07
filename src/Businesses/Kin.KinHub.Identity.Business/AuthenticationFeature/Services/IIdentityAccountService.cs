namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface IIdentityAccountService
{
    Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserProfileResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UpdateEmailAsync(
        Guid userId,
        UpdateUserEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UpdatePasswordAsync(
        Guid userId,
        UpdateUserPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LinkedProviderResponse>>> GetProvidersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LinkedProviderResponse>>> LinkProviderAsync(
        Guid userId,
        LinkProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LinkedProviderResponse>>> UnlinkProviderAsync(
        Guid userId,
        IdentityProviderType provider,
        CancellationToken cancellationToken = default);
}
