using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DA.KinHub.Functions.Http;

public sealed class ApiProblemDetailsFactory
{
    public ObjectResult Create(HttpContext httpContext, int statusCode, string title, string detail, string code)
    {
        ApiResults.ApplyNoStorePrivate(httpContext.Response);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.Extensions[ApiProblemDetailsExtensions.Code] = code;
        problem.Extensions[ApiProblemDetailsExtensions.TraceId] = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        problem.Extensions[ApiProblemDetailsExtensions.CorrelationId] = ApiResults.GetCorrelationId(httpContext);

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { ApiResults.ProblemMediaType }
        };
    }
}

public static class ApiProblemDetailsExtensions
{
    public const string Code = "code";
    public const string TraceId = "traceId";
    public const string CorrelationId = "correlationId";
}
