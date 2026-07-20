using Microsoft.AspNetCore.Authorization;

namespace AdvancedFrontier.Functions.Security;

public sealed class FamilyAuthorizationRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Family";
}
