using Microsoft.AspNetCore.Authorization;

namespace Kin.KinHub.Core.Api.Common.Authorization;

/// <summary>
/// Succeeds only when the current request is authenticated and a family context has been
/// resolved for it by <see cref="JwtAuthenticationMiddleware"/>. The precise failure reason
/// (unauthenticated, no family, Core unavailable) is derived by the result handler.
/// </summary>
public sealed class FamilyContextAuthorizationHandler
    : AuthorizationHandler<FamilyContextRequirement>
{
    private readonly ICurrentUser _currentUser;

    public FamilyContextAuthorizationHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FamilyContextRequirement requirement)
    {
        if (_currentUser.IsAuthenticated && _currentUser.HasFamilyContext)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
