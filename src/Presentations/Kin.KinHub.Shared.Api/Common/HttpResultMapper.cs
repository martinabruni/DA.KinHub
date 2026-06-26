using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CoreResult = Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Shared.Api.Common;

internal static partial class HttpResultMapper
{
    internal static IActionResult ToActionResult<T>(CoreResult.Result<T> result) =>
        result.Status switch
        {
            CoreResult.ResultStatus.Success => new OkObjectResult(result.Value),
            CoreResult.ResultStatus.NotFound => new NotFoundObjectResult(new { message = result.Message }),
            CoreResult.ResultStatus.Conflict => new ConflictObjectResult(new { message = result.Message }),
            CoreResult.ResultStatus.ValidationError => new BadRequestObjectResult(new { message = result.Message }),
            CoreResult.ResultStatus.Unauthorized => new UnauthorizedObjectResult(new { message = result.Message }),
            _ => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status500InternalServerError },
        };

    internal static IActionResult ToCreatedActionResult<T>(CoreResult.Result<T> result) =>
        result.Status switch
        {
            CoreResult.ResultStatus.Success => new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created },
            CoreResult.ResultStatus.NotFound => new NotFoundObjectResult(new { message = result.Message }),
            CoreResult.ResultStatus.Conflict => new ConflictObjectResult(new { message = result.Message }),
            CoreResult.ResultStatus.ValidationError => new BadRequestObjectResult(new { message = result.Message }),
            CoreResult.ResultStatus.Unauthorized => new UnauthorizedObjectResult(new { message = result.Message }),
            _ => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status500InternalServerError },
        };
}
