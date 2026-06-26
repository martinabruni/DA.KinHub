using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IdentityResult = Kin.KinHub.Identity.Business.Common;

namespace Kin.KinHub.Shared.Api.Common;

internal static partial class HttpResultMapper
{
    internal static IActionResult ToActionResult<T>(IdentityResult.Result<T> result) =>
        result.Status switch
        {
            IdentityResult.ResultStatus.Success => new OkObjectResult(result.Value),
            IdentityResult.ResultStatus.NotFound => new NotFoundObjectResult(new { message = result.Message }),
            IdentityResult.ResultStatus.Conflict => new ConflictObjectResult(new { message = result.Message }),
            IdentityResult.ResultStatus.ValidationError => new BadRequestObjectResult(new { message = result.Message }),
            IdentityResult.ResultStatus.Unauthorized => new UnauthorizedObjectResult(new { message = result.Message }),
            _ => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status500InternalServerError },
        };
}
