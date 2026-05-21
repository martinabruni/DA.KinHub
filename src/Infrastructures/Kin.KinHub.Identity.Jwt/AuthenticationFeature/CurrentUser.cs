
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

    /// <inheritdoc/>
    public bool IsAuthenticated { get; private set; }

    /// <inheritdoc/>
    public Guid FamilyMemberId { get; private set; }

    /// <inheritdoc/>
    public bool HasActiveMember { get; private set; }

    public void Populate(TokenClaims claims)
    {
        UserId = claims.UserId;
        Email = claims.Email;
        Roles = claims.Roles;
        IsAuthenticated = true;
    }

    public void SetFamilyMemberId(Guid memberId)
    {
        FamilyMemberId = memberId;
        HasActiveMember = true;
    }
}
