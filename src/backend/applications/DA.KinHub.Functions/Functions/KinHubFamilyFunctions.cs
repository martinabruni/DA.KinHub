using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Functions;

public sealed class KinHubFamilyFunctions(IFamilySettingsService familySettingsService, KinHubTelemetry telemetry)
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

    [RequiresFamilyAccess]
    [Function("KinHubFamilyDetails")]
    public async Task<IActionResult> GetDetails(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.KinHub.FamilyDetails)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        using var operation = telemetry.Begin(KinHubOperations.FamilyDetails);
        var result = await familySettingsService.GetFamilyDetailsAsync(authorization.RequireFamilyId(), cancellationToken);
        operation.Complete("success");
        return new OkObjectResult(result);
    }

    [RequiresFamilyAccess]
    [Function("KinHubFamilyMembers")]
    public async Task<IActionResult> GetMembers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.KinHub.FamilyMembers)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        if (!int.TryParse(request.Query["pageSize"], out var pageSize) || pageSize <= 0)
        {
            throw new BusinessValidationException(BusinessErrorCodes.PaginationPageSizeInvalid, "The pageSize query parameter must be a positive integer.");
        }

        var hasCursor = request.Query.TryGetValue("cursor", out var cursorValues) && cursorValues.Count == 1 && !string.IsNullOrWhiteSpace(cursorValues[0]);
        using var operation = telemetry.Begin(KinHubOperations.FamilyMembersPage);
        telemetry.RecordPagedRequest(KinHubOperations.FamilyMembersPage, pageSize, hasCursor, hasCursor ? "cursor" : "initial");
        var result = await familySettingsService.GetFamilyMembersPageAsync(authorization.RequireFamilyId(), pageSize, hasCursor ? cursorValues[0] : null, cancellationToken);
        telemetry.RecordPagedResult(KinHubOperations.FamilyMembersPage, result.EffectivePageSize, result.Items.Count, result.PreviousCursor is not null, result.NextCursor is not null);
        operation.Complete(result.Items.Count == 0 ? "empty" : "success");
        return new OkObjectResult(result);
    }

    [RequiresFamilyAccess]
    [Function("KinHubFamilyInvitations")]
    public async Task<IActionResult> GetInvitations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.KinHub.FamilyInvitations)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        if (!int.TryParse(request.Query["pageSize"], out var pageSize) || pageSize <= 0)
        {
            throw new BusinessValidationException(BusinessErrorCodes.PaginationPageSizeInvalid, "The pageSize query parameter must be a positive integer.");
        }

        var hasCursor = request.Query.TryGetValue("cursor", out var cursorValues) && cursorValues.Count == 1 && !string.IsNullOrWhiteSpace(cursorValues[0]);
        using var operation = telemetry.Begin(KinHubOperations.FamilyInvitationsPage);
        telemetry.RecordPagedRequest(KinHubOperations.FamilyInvitationsPage, pageSize, hasCursor, hasCursor ? "cursor" : "initial");
        var result = await familySettingsService.GetActiveFamilyInvitationsPageAsync(authorization.RequireFamilyId(), pageSize, hasCursor ? cursorValues[0] : null, cancellationToken);
        telemetry.RecordPagedResult(KinHubOperations.FamilyInvitationsPage, result.EffectivePageSize, result.Items.Count, result.PreviousCursor is not null, result.NextCursor is not null);
        operation.Complete(result.Items.Count == 0 ? "empty" : "success");
        return new OkObjectResult(result);
    }
}
