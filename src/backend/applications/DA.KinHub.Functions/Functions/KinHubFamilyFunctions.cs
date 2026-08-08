using System.Globalization;
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

public sealed class KinHubFamilyFunctions(
    IFamilySettingsService familySettingsService,
    IFamilyInvitationService familyInvitationService,
    JoinFamilyRateLimiter joinFamilyRateLimiter,
    ApiProblemDetailsFactory problemDetailsFactory,
    KinHubTelemetry telemetry)
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
        var result = await familySettingsService.GetFamilyMembersPageAsync(authorization.RequireFamilyId(), authorization.RequireApplicationUserId(), pageSize, hasCursor ? cursorValues[0] : null, cancellationToken);
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

    [RequiresFamilyAccess]
    [Function("KinHubFamilyCreateInvitation")]
    public async Task<IActionResult> CreateInvitation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.KinHub.FamilyInvitations)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        using var operation = telemetry.Begin(KinHubOperations.FamilyInvitationCreate);
        try
        {
            var result = await familyInvitationService.CreateAsync(authorization.RequireFamilyId(), authorization.RequireApplicationUserId(), cancellationToken);
            operation.Complete("created");
            return new ObjectResult(result) { StatusCode = StatusCodes.Status201Created };
        }
        catch (BusinessConflictException exception) when (exception.Code == BusinessErrorCodes.FamilyInvitationLimitReached)
        {
            telemetry.RecordSignal(KinHubOperations.FamilyInvitationCreate, "limit_reached", "validation");
            operation.Complete("limit_reached");
            throw;
        }
    }

    [RequiresFamilyAccess]
    [Function("KinHubFamilyRevokeInvitation")]
    public async Task<IActionResult> RevokeInvitation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = ApiRoutes.KinHub.FamilyInvitationById)] HttpRequest request,
        string invitationId,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        if (!Guid.TryParse(invitationId, out var parsedInvitationId) || parsedInvitationId == Guid.Empty)
        {
            throw new BusinessValidationException(BusinessErrorCodes.FamilyInvitationInvalid, "The invitation ID is invalid.");
        }

        using var operation = telemetry.Begin(KinHubOperations.FamilyInvitationRevoke);
        await familyInvitationService.RevokeAsync(authorization.RequireFamilyId(), parsedInvitationId, cancellationToken);
        operation.Complete("revoked");
        return new NoContentResult();
    }

    [Function("KinHubJoinFamily")]
    public async Task<IActionResult> JoinFamily(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.KinHub.FamilyJoin)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        var identityKey = $"{authorization.ExternalIdentity.Issuer}|{authorization.ExternalIdentity.ObjectId}";
        var originKey = ResolveOriginKey(request);
        if (!joinFamilyRateLimiter.TryAcquire(identityKey, originKey, out var retryAfterSeconds))
        {
            request.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
            telemetry.RecordSignal(KinHubOperations.FamilyInvitationJoin, "rate_limited", "throttle");
            return problemDetailsFactory.Create(request.HttpContext, StatusCodes.Status429TooManyRequests, "Too many requests", "Too many invitation join attempts were received. Try again later.", BusinessErrorCodes.FamilyInvitationRateLimited);
        }

        var body = await ReadJoinRequestAsync(request, cancellationToken);
        using var operation = telemetry.Begin(KinHubOperations.FamilyInvitationJoin);
        var result = await familyInvitationService.JoinAsync(authorization.ExternalIdentity, body.Code, cancellationToken);
        operation.Complete("joined");
        return new OkObjectResult(KinHubBootstrapResult.Family(result.FamilyId));
    }

    private static async Task<JoinFamilyRequest> ReadJoinRequestAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var body = await request.ReadFromJsonAsync<JoinFamilyRequest>(cancellationToken);
            return body ?? throw new BusinessValidationException(BusinessErrorCodes.FamilyInvitationInvalid, "Invitation code is required.");
        }
        catch (JsonException)
        {
            throw new BusinessValidationException(BusinessErrorCodes.FamilyInvitationInvalid, "Invitation code is required.");
        }
        catch (NotSupportedException)
        {
            throw new BusinessValidationException(BusinessErrorCodes.FamilyInvitationInvalid, "Invitation code is required.");
        }
    }

    private static string ResolveOriginKey(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && forwardedFor.Count > 0)
        {
            return forwardedFor[0]?.Split(',')[0].Trim() ?? "unknown";
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private sealed record JoinFamilyRequest(string? Code);
}
