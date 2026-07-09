using Kin.KinHub.Identity.Api.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Identity.Api.AccessFeature;

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
    [Authorize(Policy = FamilyContextRequirement.PolicyName)]
    public IActionResult GetFamilyContext() =>
        Ok(new
        {
            familyId = _currentUser.FamilyId,
        });
}
