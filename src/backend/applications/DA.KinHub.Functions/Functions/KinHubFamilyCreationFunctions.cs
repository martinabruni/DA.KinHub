using System.Text.Json;
using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Functions;

public sealed class KinHubFamilyCreationFunctions(
    IFamilyCreationService familyCreationService,
    KinHubTelemetry telemetry)
{
    [Function("KinHubCreateFamily")]
    public async Task<IActionResult> CreateFamily(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.KinHub.Families)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        telemetry.RecordSignal(KinHubOperations.FamilyCreation, "attempt");
        var body = await ReadRequestAsync(request, cancellationToken);
        using var operation = telemetry.Begin(KinHubOperations.FamilyCreation);

        try
        {
            var result = await familyCreationService.CreateFamilyAsync(authorization.ExternalIdentity, body.Name, cancellationToken);
            if (result.ReconciledConflict)
            {
                telemetry.RecordSignal(KinHubOperations.FamilyCreation, "concurrent_conflict", "concurrency");
            }

            operation.Complete(GetOutcome(result));

            var payload = KinHubBootstrapResult.Family(result.FamilyId);
            return result.Created
                ? new ObjectResult(payload) { StatusCode = StatusCodes.Status201Created }
                : new OkObjectResult(payload);
        }
        catch (BusinessValidationException)
        {
            telemetry.RecordSignal(KinHubOperations.FamilyCreation, "validation_rejected", "validation");
            operation.Complete("name_invalid");
            throw;
        }
        catch (BusinessDependencyException)
        {
            telemetry.RecordSignal(KinHubOperations.FamilyCreation, "postgresql_unavailable", "dependency");
            operation.Complete("postgresql_unavailable");
            throw;
        }
    }

    private static string GetOutcome(FamilyCreationResult result)
    {
        if (result.Created)
        {
            return "created";
        }

        return result.ReconciledConflict ? "existing_conflict" : "existing";
    }

    private static async Task<CreateFamilyRequest> ReadRequestAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var body = await request.ReadFromJsonAsync<CreateFamilyRequest>(cancellationToken);
            return body ?? throw new BusinessValidationException(BusinessErrorCodes.FamilyNameInvalid, "Family name is required.");
        }
        catch (JsonException)
        {
            throw new BusinessValidationException(BusinessErrorCodes.FamilyNameInvalid, "Family name is required.");
        }
        catch (NotSupportedException)
        {
            throw new BusinessValidationException(BusinessErrorCodes.FamilyNameInvalid, "Family name is required.");
        }
    }

    private sealed record CreateFamilyRequest(string? Name);
}
