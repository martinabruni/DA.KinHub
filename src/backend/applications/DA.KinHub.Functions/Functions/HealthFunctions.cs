using DA.KinHub.Functions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using DA.KinHub.Infrastructure;
using DA.KinHub.Functions.Security;

namespace DA.KinHub.Functions.Functions;

public sealed class HealthFunctions(HealthCheckService healthChecks)
{
    [AllowAnonymous]
    [Function("HealthLive")]
    public IActionResult Live([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Health.Live)] HttpRequest request)
    {
        ApiResults.ApplyNoStore(request.HttpContext.Response);
        return new OkObjectResult(new { status = "healthy", service = "KinHub" });
    }

    [AllowAnonymous]
    [Function("HealthReady")]
    public async Task<IActionResult> Ready(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Health.Ready)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResults.ApplyNoStore(request.HttpContext.Response);
        var report = await healthChecks.CheckHealthAsync(registration => registration.Tags.Contains(InfrastructureHealthChecks.ReadyTag), cancellationToken);
        var payload = new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            checks = report.Entries.Select(entry => new { name = entry.Key, status = entry.Value.Status.ToString().ToLowerInvariant() })
        };
        return report.Status == HealthStatus.Healthy
            ? new OkObjectResult(payload)
            : new ObjectResult(payload) { StatusCode = StatusCodes.Status503ServiceUnavailable };
    }
}
