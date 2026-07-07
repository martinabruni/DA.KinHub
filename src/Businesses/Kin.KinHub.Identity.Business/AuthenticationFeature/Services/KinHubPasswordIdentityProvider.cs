namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

/// <summary>
/// The built-in email + password identity provider. Wraps the KinUser / UserCredential /
/// UserProvider repositories so that the authentication handlers and the
/// provider link/unlink flow never talk to those repositories directly.
/// </summary>
public sealed class KinHubPasswordIdentityProvider : IIdentityProvider
{
    private readonly IKinUserRepository _userRepository;
    private readonly IUserCredentialRepository _credentialRepository;
    private readonly IUserProviderRepository _userProviderRepository;
    private readonly IPasswordHasher _passwordHasher;

    public KinHubPasswordIdentityProvider(
        IKinUserRepository userRepository,
        IUserCredentialRepository credentialRepository,
        IUserProviderRepository userProviderRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _userProviderRepository = userProviderRepository;
        _passwordHasher = passwordHasher;
    }

    public IdentityProviderType ProviderType => IdentityProviderType.KinHub;

    public async Task<KinUser?> AuthenticateAsync(
        IdentityCredential credential,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(credential.Email) || string.IsNullOrEmpty(credential.Password))
            return null;

        var user = await _userRepository.FindByEmailAsync(credential.Email, cancellationToken);
        if (user is null || user.Status is not UserStatus.Active)
            return null;

        var stored = await _credentialRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (stored?.PasswordHash is null)
            return null;

        return _passwordHasher.Verify(credential.Password, stored.PasswordHash)
            ? user
            : null;
    }

    public async Task<KinUser> RegisterAsync(
        IdentityRegistration registration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(registration.Password))
            throw new DomainValidationException("A password is required to register with the KinHub provider.");

        var now = DateTime.UtcNow;

        var user = new KinUser
        {
            Id = Guid.NewGuid(),
            Email = registration.Email,
            DisplayName = registration.DisplayName,
            IsEmailVerified = false,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _userRepository.CreateAsync(user, cancellationToken);

        await _credentialRepository.CreateAsync(new UserCredential
        {
            Id = Guid.NewGuid(),
            UserId = created.Id,
            PasswordHash = _passwordHasher.Hash(registration.Password),
            CreatedAt = now,
            UpdatedAt = now,
        }, cancellationToken);

        await _userProviderRepository.CreateAsync(BuildLink(created.Id, created.Id.ToString(), now), cancellationToken);

        return created;
    }

    public async Task LinkAsync(
        Guid userId,
        IdentityCredential credential,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(credential.Password))
            throw new DomainValidationException("A password is required to link the KinHub provider.");

        var now = DateTime.UtcNow;

        var existingCredential = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken);
        if (existingCredential is null)
        {
            await _credentialRepository.CreateAsync(new UserCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PasswordHash = _passwordHasher.Hash(credential.Password),
                CreatedAt = now,
                UpdatedAt = now,
            }, cancellationToken);
        }
        else
        {
            existingCredential.PasswordHash = _passwordHasher.Hash(credential.Password);
            existingCredential.UpdatedAt = now;
            await _credentialRepository.UpdateAsync(existingCredential.Id, existingCredential, cancellationToken);
        }

        await _userProviderRepository.CreateAsync(BuildLink(userId, userId.ToString(), now), cancellationToken);
    }

    public async Task UnlinkAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var link = await _userProviderRepository.GetByUserAndProviderAsync(userId, (int)ProviderType, cancellationToken);
        if (link is null)
            return;

        await _userProviderRepository.DeleteAsync(link.Id, cancellationToken);

        var credential = await _credentialRepository.GetByUserIdAsync(userId, cancellationToken);
        if (credential is not null)
            await _credentialRepository.DeleteAsync(credential.Id, cancellationToken);
    }

    private static UserProvider BuildLink(Guid userId, string providerUserId, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProviderId = (int)IdentityProviderType.KinHub,
            ProviderUserId = providerUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
}
