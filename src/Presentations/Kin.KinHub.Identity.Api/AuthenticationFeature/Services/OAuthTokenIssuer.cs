namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public interface IOAuthTokenIssuer
{
    object CreateScopedTokenResponse(LoginResponse response, string scope);
}

public sealed class OAuthTokenIssuer : IOAuthTokenIssuer
{
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenValidator _tokenValidator;

    public OAuthTokenIssuer(ITokenGenerator tokenGenerator, ITokenValidator tokenValidator)
    {
        _tokenGenerator = tokenGenerator;
        _tokenValidator = tokenValidator;
    }

    public object CreateScopedTokenResponse(LoginResponse response, string scope)
    {
        var claims = _tokenValidator.ValidateAccessToken(response.AccessToken);
        if (claims is null)
        {
            throw new InvalidOperationException("Unable to validate issued access token.");
        }

        var user = new KinUser
        {
            Id = claims.UserId,
            Email = claims.Email,
            DisplayName = response.DisplayName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var accessToken = _tokenGenerator.GenerateAccessToken(
            user,
            claims.Roles,
            scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = response.ExpiresIn,
            scope,
        };
    }
}
