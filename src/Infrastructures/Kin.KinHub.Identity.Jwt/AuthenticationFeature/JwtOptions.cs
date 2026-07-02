namespace Kin.KinHub.Identity.Jwt.AuthenticationFeature;

public sealed class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = "kinhub.api";
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret) || Secret.Length < 32)
            throw new InvalidOperationException(
                $"{nameof(Secret)} must be configured and at least 32 characters long.");

        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException($"{nameof(Issuer)} must be configured.");

        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException($"{nameof(Audience)} must be configured.");
    }
}
