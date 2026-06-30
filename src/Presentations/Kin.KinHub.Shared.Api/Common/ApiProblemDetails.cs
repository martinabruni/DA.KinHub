using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.Common;

internal static class ApiProblemDetails
{
    internal static IActionResult InvalidRequestBody(ControllerBase controller) =>
        BadRequest(controller, "invalid_request_body", "Invalid request body.");

    internal static IActionResult Validation(ControllerBase controller, IReadOnlyList<string> errors) =>
        BadRequest(controller, "validation_error", errors);

    internal static IActionResult AuthenticationRequired(ControllerBase controller) =>
        Unauthorized(controller, "authentication_required", "Missing or invalid Authorization header.");

    internal static IActionResult BadRequest(ControllerBase controller, string code, string detail) =>
        controller.BadRequest(Create(controller, StatusCodes.Status400BadRequest, code, detail));

    internal static IActionResult BadRequest(ControllerBase controller, string code, IReadOnlyList<string> errors) =>
        controller.BadRequest(Create(controller, StatusCodes.Status400BadRequest, code, "One or more validation errors occurred.", errors));

    internal static IActionResult Unauthorized(ControllerBase controller, string code, string detail) =>
        controller.Unauthorized(Create(controller, StatusCodes.Status401Unauthorized, code, detail));

    internal static IActionResult Forbidden(ControllerBase controller, string code, string detail) =>
        new ObjectResult(Create(controller, StatusCodes.Status403Forbidden, code, detail))
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };

    internal static IActionResult ServiceUnavailable(ControllerBase controller, string code, string detail) =>
        new ObjectResult(Create(controller, StatusCodes.Status503ServiceUnavailable, code, detail))
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable,
        };

    internal static ProblemDetails Create(
        ControllerBase controller,
        int status,
        string code,
        string detail,
        IReadOnlyList<string>? errors = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = GetTitle(status),
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = controller.HttpContext.Request.Path,
        };

        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = controller.HttpContext.TraceIdentifier;

        if (errors is not null && errors.Count > 0)
        {
            problem.Extensions["errors"] = errors;
        }

        return problem;
    }

    private static string GetTitle(int status) =>
        status switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
            _ => "Error",
        };
}
