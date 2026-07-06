using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IdentityResult = Kin.KinHub.Identity.Business.Common;

namespace Kin.KinHub.Shared.Api.Common;

public static partial class HttpResultMapper
{
    public static IActionResult ToActionResult<T>(IdentityResult.Result<T> result) =>
        result.Status switch
        {
            ResultStatus.Success => new OkObjectResult(result.Value),
            ResultStatus.NotFound => new NotFoundObjectResult(new { message = result.Message }),
            ResultStatus.Conflict => new ConflictObjectResult(new { message = result.Message }),
            ResultStatus.ValidationError => new BadRequestObjectResult(new { message = result.Message }),
            ResultStatus.UnprocessableEntity => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status422UnprocessableEntity },
            ResultStatus.Unauthorized => new UnauthorizedObjectResult(new { message = result.Message }),
            ResultStatus.ServiceUnavailable => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status503ServiceUnavailable },
            _ => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status500InternalServerError },
        };

    public static IActionResult ToActionResult<T>(ControllerBase controller, IdentityResult.Result<T> result) =>
        SharedHttpResultMapper.ToActionResult(controller, result, unauthorizedIsForbidden: false);

    public static IActionResult ToCreatedActionResult<T>(ControllerBase controller, IdentityResult.Result<T> result) =>
        SharedHttpResultMapper.ToCreatedActionResult(controller, result, unauthorizedIsForbidden: false);
}
