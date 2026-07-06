using Kin.KinHub.Shared.Kernel.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.Common;

public static class SharedHttpResultMapper
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, IResult<T> result, bool unauthorizedIsForbidden) =>
        result.Status switch
        {
            ResultStatus.Success => new OkObjectResult(result.Value),
            ResultStatus.NotFound => new NotFoundObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status404NotFound, result.Code ?? "not_found", result.Message ?? "Resource not found.")),
            ResultStatus.Conflict => new ConflictObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status409Conflict, result.Code ?? "conflict", result.Message ?? "The request conflicted with the current resource state.")),
            ResultStatus.ValidationError => new BadRequestObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status400BadRequest, result.Code ?? "validation_error", result.Message ?? "The request is invalid.")),
            ResultStatus.UnprocessableEntity => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status422UnprocessableEntity, result.Code ?? "unprocessable_entity", result.Message ?? "The request could not be processed.")) { StatusCode = StatusCodes.Status422UnprocessableEntity },
            ResultStatus.Unauthorized when unauthorizedIsForbidden => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status403Forbidden, result.Code ?? "forbidden", result.Message ?? "The authenticated user cannot access this resource.")) { StatusCode = StatusCodes.Status403Forbidden },
            ResultStatus.Unauthorized => new UnauthorizedObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status401Unauthorized, result.Code ?? "authentication_required", result.Message ?? "Missing or invalid Authorization header.")),
            ResultStatus.ServiceUnavailable => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status503ServiceUnavailable, result.Code ?? "service_unavailable", result.Message ?? "A required upstream service is unavailable.")) { StatusCode = StatusCodes.Status503ServiceUnavailable },
            _ => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status500InternalServerError, result.Code ?? "unexpected_error", result.Message ?? "Unexpected server error.")) { StatusCode = StatusCodes.Status500InternalServerError },
        };

    public static IActionResult ToCreatedActionResult<T>(ControllerBase controller, IResult<T> result, bool unauthorizedIsForbidden) =>
        result.Status is ResultStatus.Success
            ? new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created }
            : ToActionResult(controller, result, unauthorizedIsForbidden);
}
