using Kin.KinHub.KinList.Business.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.KinList.Api.Common;

internal static class KinListHttpResultMapper
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, Result<T> result) =>
        result.Status switch
        {
            ResultStatus.Success => new OkObjectResult(result.Value),
            ResultStatus.NotFound => new NotFoundObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status404NotFound, result.Code ?? "not_found", result.Message ?? "Resource not found.")),
            ResultStatus.Conflict => new ConflictObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status409Conflict, result.Code ?? "conflict", result.Message ?? "The request conflicted with the current resource state.")),
            ResultStatus.ValidationError => new BadRequestObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status400BadRequest, result.Code ?? "validation_error", result.Message ?? "The request is invalid.")),
            ResultStatus.UnprocessableEntity => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status422UnprocessableEntity, result.Code ?? "unprocessable_entity", result.Message ?? "The request could not be processed.")) { StatusCode = StatusCodes.Status422UnprocessableEntity },
            ResultStatus.Unauthorized => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status403Forbidden, result.Code ?? "forbidden", result.Message ?? "The authenticated user cannot access this resource.")) { StatusCode = StatusCodes.Status403Forbidden },
            ResultStatus.ServiceUnavailable => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status503ServiceUnavailable, result.Code ?? "service_unavailable", result.Message ?? "A required upstream service is unavailable.")) { StatusCode = StatusCodes.Status503ServiceUnavailable },
            _ => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status500InternalServerError, result.Code ?? "unexpected_error", result.Message ?? "Unexpected server error.")) { StatusCode = StatusCodes.Status500InternalServerError },
        };

    public static IActionResult ToCreatedActionResult<T>(ControllerBase controller, Result<T> result) =>
        result.Status switch
        {
            ResultStatus.Success => new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created },
            _ => ToActionResult(controller, result),
        };
}
