using DA.KinHub.Business.Identity;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Functions;

public sealed class KinHubServicesFunctions(
    IKinHubServiceCatalogService serviceCatalogService,
    IKinHubServiceAccessService serviceAccessService,
    KinHubTelemetry telemetry)
{
    [RequiresFamilyAccess]
    [Function("KinHubServiceCatalog")]
    public async Task<IActionResult> GetCatalog(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.KinHub.Services)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        using var operation = telemetry.Begin(KinHubOperations.ServiceCatalog);
        var result = await serviceCatalogService.GetCatalogAsync(authorization.RequireFamilyId(), request.Query["language"], cancellationToken);
        operation.Complete(result.Services.Count == 0 ? "empty" : "success");
        return new OkObjectResult(result);
    }

    [RequiresFamilyAccess]
    [Function("KinHubServiceAccess")]
    public async Task<IActionResult> CheckAccess(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.KinHub.ServiceAccess)] HttpRequest request,
        string serviceKey,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        using var operation = telemetry.Begin(KinHubOperations.ServiceAccess);
        await serviceAccessService.EnsureAccessAsync(authorization.RequireFamilyId(), serviceKey, cancellationToken);
        operation.Complete("granted");
        return new NoContentResult();
    }
}
