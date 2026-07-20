using Microsoft.AspNetCore.Authorization;

namespace DA.KinHub.Functions.Security;

public sealed class FamilyAuthorizationRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "Family";
}
