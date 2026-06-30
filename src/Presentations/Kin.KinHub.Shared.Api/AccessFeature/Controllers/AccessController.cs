using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.AccessFeature;

[ApiController]
[Route("api/access")]
public sealed class AccessController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    public AccessController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("family-context")]
    public IActionResult GetFamilyContext()
    {
        if (!_currentUser.IsAuthenticated)
        {
            return ApiProblemDetails.Unauthorized(this, "authentication_required", "Missing or invalid Authorization header.");
        }

        if (!_currentUser.HasFamilyContext)
        {
            return ApiProblemDetails.Forbidden(this, "family_required", "The authenticated user does not currently belong to a family.");
        }

        return Ok(new
        {
            familyId = _currentUser.FamilyId,
        });
    }
}
