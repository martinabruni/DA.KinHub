
namespace Kin.KinHub.Identity.Jwt.AuthenticationFeature;

/// <inheritdoc/>
public sealed class CurrentUser : ICurrentUser
{
    /// <inheritdoc/>
    public Guid UserId { get; private set; }

    /// <inheritdoc/>
    public string Email { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public IReadOnlyList<string> Roles { get; private set; } = [];

    public IReadOnlyList<string> Scopes { get; private set; } = [];

    /// <inheritdoc/>
    public bool IsAuthenticated { get; private set; }

    /// <inheritdoc/>
    public Guid FamilyId { get; private set; }

    /// <inheritdoc/>
    public bool HasFamilyContext { get; private set; }

    public void Populate(TokenClaims claims)
    {
        UserId = claims.UserId;
        Email = claims.Email;
        Roles = claims.Roles;
        Scopes = claims.Scopes;
        IsAuthenticated = true;
    }

    public void SetFamilyContext(Guid familyId)
    {
        FamilyId = familyId;
        HasFamilyContext = true;
    }
}
