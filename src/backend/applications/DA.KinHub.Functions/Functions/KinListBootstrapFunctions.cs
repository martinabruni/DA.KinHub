using DA.KinHub.Business.Identity;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Functions;

public sealed class KinListBootstrapFunctions(
    IKinListBootstrapService bootstrapService,
    KinListTelemetry telemetry)
{
    [Function("KinListBootstrap")]
    public async Task<IActionResult> Bootstrap(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.KinList.Bootstrap)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        using var operation = telemetry.Begin(KinListOperations.Bootstrap);
        var result = await bootstrapService.GetBootstrapAsync(authorization.ExternalIdentity, cancellationToken);
        operation.Complete(result.State);
        return new OkObjectResult(result);
    }
}
