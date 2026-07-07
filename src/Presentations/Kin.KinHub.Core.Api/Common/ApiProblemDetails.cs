using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Core.Api.Common;

public static class ApiProblemDetails
{
    public static IActionResult InvalidRequestBody(ControllerBase controller) =>
        BadRequest(controller, "invalid_request_body", "Invalid request body.");

    public static IActionResult Validation(ControllerBase controller, IReadOnlyList<string> errors) =>
        BadRequest(controller, "validation_error", errors);

    public static IActionResult AuthenticationRequired(ControllerBase controller) =>
        Unauthorized(controller, "authentication_required", "Missing or invalid Authorization header.");

    public static IActionResult BadRequest(ControllerBase controller, string code, string detail) =>
        controller.BadRequest(Create(controller, StatusCodes.Status400BadRequest, code, detail));

    public static IActionResult BadRequest(ControllerBase controller, string code, IReadOnlyList<string> errors) =>
        controller.BadRequest(Create(controller, StatusCodes.Status400BadRequest, code, "One or more validation errors occurred.", errors));

    public static IActionResult Unauthorized(ControllerBase controller, string code, string detail) =>
        controller.Unauthorized(Create(controller, StatusCodes.Status401Unauthorized, code, detail));

    public static IActionResult Forbidden(ControllerBase controller, string code, string detail) =>
        new ObjectResult(Create(controller, StatusCodes.Status403Forbidden, code, detail))
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };

    public static IActionResult ServiceUnavailable(ControllerBase controller, string code, string detail) =>
        new ObjectResult(Create(controller, StatusCodes.Status503ServiceUnavailable, code, detail))
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable,
        };

    public static ProblemDetails Create(
        ControllerBase controller,
        int status,
        string code,
        string detail,
        IReadOnlyList<string>? errors = null) =>
        Create(controller.HttpContext, status, code, detail, errors);

    public static ProblemDetails Create(
        HttpContext httpContext,
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
            Instance = httpContext.Request.Path,
        };

        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = httpContext.TraceIdentifier;

        if (errors is not null && errors.Count > 0)
        {
            problem.Extensions["errors"] = errors;
        }

        return problem;
    }

    /// <summary>
    /// Writes an RFC 9457 problem detail directly to the response (for use outside MVC actions,
    /// e.g. authorization middleware).
    /// </summary>
    public static Task WriteAsync(
        HttpContext httpContext,
        int status,
        string code,
        string detail)
    {
        var problem = Create(httpContext, status, code, detail);
        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        return httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
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
