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
            CoreResult.ResultStatus.Unauthorized => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status500InternalServerError },
        };

    internal static IActionResult ToActionResult<T>(ControllerBase controller, CoreResult.Result<T> result) =>
        result.Status switch
        {
            CoreResult.ResultStatus.Success => new OkObjectResult(result.Value),
            CoreResult.ResultStatus.NotFound => new NotFoundObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status404NotFound, "not_found", result.Message ?? "Resource not found.")),
            CoreResult.ResultStatus.Conflict => new ConflictObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status409Conflict, "conflict", result.Message ?? "The request conflicted with the current resource state.")),
            CoreResult.ResultStatus.ValidationError => new BadRequestObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status400BadRequest, "validation_error", result.Message ?? "The request is invalid.")),
            CoreResult.ResultStatus.Unauthorized => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status403Forbidden, "forbidden", result.Message ?? "The authenticated user cannot access this resource.")) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status500InternalServerError, "unexpected_error", result.Message ?? "Unexpected server error.")) { StatusCode = StatusCodes.Status500InternalServerError },
        };

    internal static IActionResult ToCreatedActionResult<T>(CoreResult.Result<T> result) =>
        result.Status switch
        {
            CoreResult.ResultStatus.Success => new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created },
            _ => ToActionResult(result),
        };

    internal static IActionResult ToCreatedActionResult<T>(ControllerBase controller, CoreResult.Result<T> result) =>
        result.Status switch
        {
            CoreResult.ResultStatus.Success => new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created },
            _ => ToActionResult(controller, result),
        };
}
