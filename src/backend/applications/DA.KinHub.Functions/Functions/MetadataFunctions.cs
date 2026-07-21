using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Functions.Functions;

public sealed class MetadataFunctions(
    BuildInfoProvider buildInfoProvider,
    TimeProvider timeProvider,
    IOptions<EntraOptions> entraOptions)
{
    [Function("Version")]
    public IActionResult Version([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/version")] HttpRequest request)
    {
        ApiResults.ApplyCorrelationId(request);
        return new OkObjectResult(buildInfoProvider.Get());
    }

    [Function("Status")]
    public IActionResult Status([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/status")] HttpRequest request)
    {
        ApiResults.ApplyCorrelationId(request);
        var build = buildInfoProvider.Get();
        return new OkObjectResult(new
        {
            status = "operational",
            appName = build.AppName,
            version = build.Version,
            environment = build.Environment,
            timestamp = timeProvider.GetUtcNow()
        });
    }

    [Function("OpenApi")]
    public IActionResult OpenApi([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/openapi.json")] HttpRequest request)
    {
        ApiResults.ApplyCorrelationId(request);
        var entra = entraOptions.Value;
        var authority = $"{entra.Instance.TrimEnd('/')}/{entra.TenantId}/oauth2/v2.0";
        var apiScope = entra.Scope.StartsWith("api://", StringComparison.OrdinalIgnoreCase)
            ? entra.Scope
            : $"{entra.Audience.TrimEnd('/')}/{entra.Scope}";

        return new OkObjectResult(new
        {
            openapi = "3.0.3",
            info = new { title = "KinHub API", version = buildInfoProvider.Get().ApiVersion },
            paths = new Dictionary<string, object>
            {
                ["/health/live"] = new { get = new { summary = "Liveness", responses = new Dictionary<string, object> { ["200"] = new { description = "Healthy" } } } },
                ["/health/ready"] = new { get = new { summary = "Readiness", responses = new Dictionary<string, object> { ["200"] = new { description = "Ready" }, ["503"] = new { description = "Not ready" } } } },
                ["/api/version"] = new { get = new { summary = "Build metadata", responses = new Dictionary<string, object> { ["200"] = new { description = "Version" } } } },
                ["/api/kinlist/bootstrap"] = new { get = new { summary = "Resolve the KinList post-login state" } },
                ["/api/kinlist/family-context"] = new { get = new { summary = "Validate the Family policy for a familyId" } }
            },
            components = new
            {
                securitySchemes = new Dictionary<string, object>
                {
                    ["bearerAuth"] = new { type = "http", scheme = "bearer", bearerFormat = "JWT" },
                    ["entraOAuth2"] = new
                    {
                        type = "oauth2",
                        flows = new
                        {
                            authorizationCode = new
                            {
                                authorizationUrl = $"{authority}/authorize",
                                tokenUrl = $"{authority}/token",
                                scopes = new Dictionary<string, string> { [apiScope] = "Access KinHub as the signed-in user" }
                            }
                        }
                    }
                }
            }
        });
    }
}
