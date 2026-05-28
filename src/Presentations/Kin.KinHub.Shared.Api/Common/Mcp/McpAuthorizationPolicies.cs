using System.Security.Claims;

namespace Kin.KinHub.Shared.Api.Common.Mcp;

public static class McpScopes
{
    public const string Read = "mcp:read";
    public const string Write = "mcp:write";
    public const string Admin = "mcp:admin";
}

public static class McpAuthorizationPolicies
{
    public const string Read = "McpRead";
    public const string Write = "McpWrite";
    public const string Admin = "McpAdmin";

    public static bool HasAnyScope(ClaimsPrincipal user, params string[] allowedScopes)
    {
        var grantedScopes = user.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.Ordinal);

        return grantedScopes.Overlaps(allowedScopes);
    }
}
