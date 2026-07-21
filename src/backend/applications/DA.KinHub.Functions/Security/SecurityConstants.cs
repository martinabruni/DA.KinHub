using System.IdentityModel.Tokens.Jwt;

namespace DA.KinHub.Functions.Security;

public static class SecurityConstants
{
    public const string ApiAccessPolicy = "ApiAccess";
    public const string FamilyPolicy = "Family";
    public const string BearerScheme = "bearerAuth";
    public const string ScopeClaim = "scp";
    public const string LegacyScopeClaim = "http://schemas.microsoft.com/identity/claims/scope";
    public const string ObjectIdClaim = "oid";
    public const string IssuerClaim = JwtRegisteredClaimNames.Iss;
    public const string FamilyIdQueryParameter = "familyId";
}
