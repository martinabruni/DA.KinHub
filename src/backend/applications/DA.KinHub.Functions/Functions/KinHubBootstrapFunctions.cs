using DA.KinHub.Business.Identity;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Functions;

public sealed class KinHubBootstrapFunctions(
    IKinHubBootstrapService bootstrapService,
    KinHubTelemetry telemetry)
{
    [Function("KinHubBootstrap")]
    public async Task<IActionResult> Bootstrap(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.KinHub.Bootstrap)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        using var operation = telemetry.Begin(KinHubOperations.Bootstrap);
        var result = await bootstrapService.GetBootstrapAsync(authorization.ExternalIdentity, cancellationToken);
        operation.Complete(result.State);
        return new OkObjectResult(result);
    }
}
