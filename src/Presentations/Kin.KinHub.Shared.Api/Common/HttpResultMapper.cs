using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CoreResult = Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Shared.Api.Common;

public static partial class HttpResultMapper
{
    public static IActionResult ToActionResult<T>(CoreResult.Result<T> result) =>
        result.Status switch
        {
            ResultStatus.Success => new OkObjectResult(result.Value),
            ResultStatus.NotFound => new NotFoundObjectResult(new { message = result.Message }),
            ResultStatus.Conflict => new ConflictObjectResult(new { message = result.Message }),
            ResultStatus.ValidationError => new BadRequestObjectResult(new { message = result.Message }),
            ResultStatus.UnprocessableEntity => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status422UnprocessableEntity },
            ResultStatus.Unauthorized => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status403Forbidden },
            ResultStatus.ServiceUnavailable => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status503ServiceUnavailable },
            _ => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status500InternalServerError },
        };

    public static IActionResult ToActionResult<T>(ControllerBase controller, CoreResult.Result<T> result) =>
        SharedHttpResultMapper.ToActionResult(controller, result, unauthorizedIsForbidden: true);

    public static IActionResult ToCreatedActionResult<T>(CoreResult.Result<T> result) =>
        result.Status switch
        {
            ResultStatus.Success => new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created },
            _ => ToActionResult(result),
        };

    public static IActionResult ToCreatedActionResult<T>(ControllerBase controller, CoreResult.Result<T> result) =>
        SharedHttpResultMapper.ToCreatedActionResult(controller, result, unauthorizedIsForbidden: true);
}
