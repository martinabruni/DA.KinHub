namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public interface IOAuthRefreshTokenScopeStore
{
    void Store(string refreshToken, string scope);
    bool TryGet(string refreshToken, out string? scope);
    void Replace(string previousRefreshToken, string nextRefreshToken, string scope);
}
