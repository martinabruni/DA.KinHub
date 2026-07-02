using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IdentityResult = Kin.KinHub.Identity.Business.Common;

namespace Kin.KinHub.Shared.Api.Common;

public static partial class HttpResultMapper
{
    public static IActionResult ToActionResult<T>(IdentityResult.Result<T> result) =>
        result.Status switch
        {
            IdentityResult.ResultStatus.Success => new OkObjectResult(result.Value),
            IdentityResult.ResultStatus.NotFound => new NotFoundObjectResult(new { message = result.Message }),
            IdentityResult.ResultStatus.Conflict => new ConflictObjectResult(new { message = result.Message }),
            IdentityResult.ResultStatus.ValidationError => new BadRequestObjectResult(new { message = result.Message }),
            IdentityResult.ResultStatus.Unauthorized => new UnauthorizedObjectResult(new { message = result.Message }),
            _ => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status500InternalServerError },
        };

    public static IActionResult ToActionResult<T>(ControllerBase controller, IdentityResult.Result<T> result) =>
        result.Status switch
        {
            IdentityResult.ResultStatus.Success => new OkObjectResult(result.Value),
            IdentityResult.ResultStatus.NotFound => new NotFoundObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status404NotFound, "not_found", result.Message ?? "Resource not found.")),
            IdentityResult.ResultStatus.Conflict => new ConflictObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status409Conflict, "conflict", result.Message ?? "The request conflicted with the current resource state.")),
            IdentityResult.ResultStatus.ValidationError => new BadRequestObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status400BadRequest, "validation_error", result.Message ?? "The request is invalid.")),
            IdentityResult.ResultStatus.Unauthorized => new UnauthorizedObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status401Unauthorized, "authentication_required", result.Message ?? "Missing or invalid Authorization header.")),
            _ => new ObjectResult(ApiProblemDetails.Create(controller, StatusCodes.Status500InternalServerError, "unexpected_error", result.Message ?? "Unexpected server error.")) { StatusCode = StatusCodes.Status500InternalServerError },
        };

    public static IActionResult ToCreatedActionResult<T>(ControllerBase controller, IdentityResult.Result<T> result) =>
        result.Status switch
        {
            IdentityResult.ResultStatus.Success => new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created },
            _ => ToActionResult(controller, result),
        };
}
