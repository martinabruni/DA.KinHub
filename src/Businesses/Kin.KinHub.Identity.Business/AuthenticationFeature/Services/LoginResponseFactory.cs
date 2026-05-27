namespace Kin.KinHub.Identity.Business.AuthenticationFeature;

public interface ILoginResponseFactory
{
    Task<LoginResponse> CreateAsync(
        KinUser user,
        CancellationToken cancellationToken = default);
}

public sealed class LoginResponseFactory : ILoginResponseFactory
{
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginResponseFactory(
        ITokenGenerator tokenGenerator,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _tokenGenerator = tokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<LoginResponse> CreateAsync(
        KinUser user,
        CancellationToken cancellationToken = default)
    {
        var accessToken = _tokenGenerator.GenerateAccessToken(user, []);
        var rawRefreshToken = _tokenGenerator.GenerateRefreshToken();

        var now = DateTime.UtcNow;
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = rawRefreshToken,
            ExpiresAtUtc = now.AddDays(7),
            Revoked = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresIn = _tokenGenerator.AccessTokenExpirySeconds,
            Email = user.Email,
            DisplayName = user.DisplayName,
        };
    }
}
