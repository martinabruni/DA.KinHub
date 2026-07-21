using DA.KinHub.Functions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DA.KinHub.Functions.Functions;

public sealed class HealthFunctions(HealthCheckService healthChecks)
{
    [Function("HealthLive")]
    public IActionResult Live([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/live")] HttpRequest request)
    {
        ApiResults.ApplyCorrelationId(request);
        return new OkObjectResult(new { status = "healthy", service = "KinHub" });
    }

    [Function("HealthReady")]
    public async Task<IActionResult> Ready(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health/ready")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResults.ApplyCorrelationId(request);
        var report = await healthChecks.CheckHealthAsync(registration => registration.Tags.Contains("ready"), cancellationToken);
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
