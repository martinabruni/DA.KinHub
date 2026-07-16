using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AdvancedFrontier.Functions.Http;

public static class ApiResults
{
    public static string ApplyCorrelationId(HttpRequest request)
    {
        var correlationId = request.Headers.TryGetValue("X-Correlation-ID", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : request.HttpContext.TraceIdentifier;
        request.HttpContext.Response.Headers["X-Correlation-ID"] = correlationId;
        return correlationId;
    }

    public static ObjectResult Problem(HttpRequest request, int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = ApplyCorrelationId(request);
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
