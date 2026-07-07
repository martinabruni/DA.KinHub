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

/// <summary>
/// Manages the identity providers linked to a user. Enforces the invariants that a user
/// always keeps at least one provider and that providers are never auto-linked by email.
/// </summary>
public sealed class UserProviderService : IUserProviderService
{
    private readonly IUserProviderRepository _userProviderRepository;
    private readonly IIdentityProviderRegistry _providerRegistry;

    public UserProviderService(
        IUserProviderRepository userProviderRepository,
        IIdentityProviderRegistry providerRegistry)
    {
        _userProviderRepository = userProviderRepository;
        _providerRegistry = providerRegistry;
    }

    public async Task<Result<IReadOnlyList<LinkedProviderResponse>>> GetProvidersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var links = await _userProviderRepository.GetByUserIdAsync(userId);
        return Result<IReadOnlyList<LinkedProviderResponse>>.Success(Map(links));
    }

    public async Task<Result<IReadOnlyList<LinkedProviderResponse>>> LinkAsync(
        Guid userId,
        LinkProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerRegistry.Resolve(request.Provider);
        if (provider is null)
            return Result<IReadOnlyList<LinkedProviderResponse>>.ValidationError(
                $"The '{request.Provider}' identity provider is not supported.");

        var existing = await _userProviderRepository.GetByUserAndProviderAsync(userId, (int)request.Provider);
        if (existing is not null)
            return Result<IReadOnlyList<LinkedProviderResponse>>.Conflict(
                $"The '{request.Provider}' provider is already linked to this account.");

        try
        {
            await provider.LinkAsync(
                userId,
                new IdentityCredential
                {
                    Password = request.Password,
                    ExternalToken = request.ExternalToken,
                },
                cancellationToken);
        }
        catch (DuplicateEntityException ex)
        {
            return Result<IReadOnlyList<LinkedProviderResponse>>.Conflict(ex.Message);
        }
        catch (DomainValidationException ex)
        {
            return Result<IReadOnlyList<LinkedProviderResponse>>.ValidationError(ex.Message);
        }
        catch (SharedDomainException)
        {
            return Result<IReadOnlyList<LinkedProviderResponse>>.UnexpectedError("Failed to link provider.");
        }

        var links = await _userProviderRepository.GetByUserIdAsync(userId);
        return Result<IReadOnlyList<LinkedProviderResponse>>.Success(Map(links));
    }

    public async Task<Result<IReadOnlyList<LinkedProviderResponse>>> UnlinkAsync(
        Guid userId,
        IdentityProviderType provider,
        CancellationToken cancellationToken = default)
    {
        var adapter = _providerRegistry.Resolve(provider);
        if (adapter is null)
            return Result<IReadOnlyList<LinkedProviderResponse>>.ValidationError(
                $"The '{provider}' identity provider is not supported.");

        var links = await _userProviderRepository.GetByUserIdAsync(userId);

        if (links.All(link => link.ProviderId != (int)provider))
            return Result<IReadOnlyList<LinkedProviderResponse>>.NotFound(
                $"The '{provider}' provider is not linked to this account.");

        if (links.Count <= 1)
            return Result<IReadOnlyList<LinkedProviderResponse>>.ValidationError(
                "Cannot unlink the last remaining identity provider.");

        try
        {
            await adapter.UnlinkAsync(userId, cancellationToken);
        }
        catch (SharedDomainException)
        {
            return Result<IReadOnlyList<LinkedProviderResponse>>.UnexpectedError("Failed to unlink provider.");
        }

        var remaining = await _userProviderRepository.GetByUserIdAsync(userId);
        return Result<IReadOnlyList<LinkedProviderResponse>>.Success(Map(remaining));
    }

    private static IReadOnlyList<LinkedProviderResponse> Map(IReadOnlyList<UserProvider> links) =>
        links
            .Select(link => new LinkedProviderResponse
            {
                Provider = (IdentityProviderType)link.ProviderId,
                ProviderName = Enum.IsDefined(typeof(IdentityProviderType), link.ProviderId)
                    ? ((IdentityProviderType)link.ProviderId).ToString()
                    : link.ProviderId.ToString(),
                LinkedAt = link.CreatedAt,
            })
            .ToList();
}
