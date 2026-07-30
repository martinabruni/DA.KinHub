using DA.KinHub.Business.Common;
using DA.KinHub.Business.KinList;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DA.KinHub.Functions.Functions;

public sealed class KinListFunctions(
    IActiveItemsPageService activeItemsPageService,
    KinHubTelemetry telemetry)
{
    [RequiresFamilyAccess]
    [Function("KinListItems")]
    public async Task<IActionResult> GetItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.KinList.Items)] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Features.Get<KinHubAuthorizationFeature>()
            ?? throw new InvalidOperationException("Authorized request feature is missing.");

        if (!int.TryParse(request.Query["pageSize"], out var pageSize) || pageSize <= 0)
        {
            throw new BusinessValidationException(BusinessErrorCodes.PaginationPageSizeInvalid, "The pageSize query parameter must be a positive integer.");
        }

        var hasCursor = request.Query.TryGetValue("cursor", out var cursorValues) && cursorValues.Count == 1 && !string.IsNullOrWhiteSpace(cursorValues[0]);

        using var operation = telemetry.Begin(KinHubOperations.KinListItemsPage);
        telemetry.RecordItemsPageRequest(pageSize, hasCursor);
        var result = await activeItemsPageService.GetActiveItemsPageAsync(
            authorization.RequireApplicationUserId(),
            authorization.RequireFamilyId(),
            pageSize,
            hasCursor ? cursorValues[0] : null,
            cancellationToken);
        telemetry.RecordItemsPageResult(result.EffectivePageSize, result.PreviousCursor is not null, result.NextCursor is not null);
        operation.Complete(result.Items.Count == 0 ? "empty" : "success");
        return new OkObjectResult(result);
    }
}
