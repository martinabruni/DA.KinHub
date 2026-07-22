using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Functions;

public sealed class KinHubFamilyFunctions(KinHubTelemetry telemetry)
{
    [RequiresFamilyAccess]
    [Function("KinHubFamilyContext")]
    public async Task<IActionResult> FamilyContext(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.KinHub.FamilyContext)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        _ = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        using var operation = telemetry.Begin(KinHubOperations.FamilyAuthorization);
        await Task.CompletedTask;
        operation.Complete("granted");
        return new NoContentResult();
    }
}
