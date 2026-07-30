using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Functions;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DA.KinHub.IntegrationTests;

public sealed class KinHubServicesFunctionsTests
{
    [Fact]
    public async Task GetCatalogReturnsServicesPayload()
    {
        var functions = new KinHubServicesFunctions(
            new StubCatalogService(new KinHubServiceCatalogResult([new KinHubServiceCatalogItem("kinlist", "/kinlist", "KinList", "Shared list") ])),
            new AllowAccessService(),
            CreateTelemetry());
        var request = CreateRequest("/api/kinhub/services?familyId=11111111-1111-1111-1111-111111111111&language=it", Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var result = await functions.GetCatalog(request, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<KinHubServiceCatalogResult>(response.Value);
        Assert.Single(payload.Services);
        Assert.Equal("kinlist", payload.Services[0].Key);
    }

    [Fact]
    public async Task CheckAccessReturns204WhenGranted()
    {
        var functions = new KinHubServicesFunctions(
            new StubCatalogService(new KinHubServiceCatalogResult([])),
            new AllowAccessService(),
            CreateTelemetry());
        var request = CreateRequest("/api/kinhub/services/kinlist/access?familyId=11111111-1111-1111-1111-111111111111", Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var result = await functions.CheckAccess(request, "kinlist", CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task CheckAccessPropagatesForbiddenCode()
    {
        var functions = new KinHubServicesFunctions(
            new StubCatalogService(new KinHubServiceCatalogResult([])),
            new DenyAccessService(),
            CreateTelemetry());
        var request = CreateRequest("/api/kinhub/services/kinlist/access?familyId=11111111-1111-1111-1111-111111111111", Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var exception = await Assert.ThrowsAsync<BusinessAccessDeniedException>(() => functions.CheckAccess(request, "kinlist", CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.ServiceAccessDenied, exception.Code);
    }

    private static KinHubTelemetry CreateTelemetry()
        => new(new BuildInfoProvider(Options.Create(new RuntimeOptions { AppName = "KinHub", ApiVersion = "1.0", Environment = "Test" })));

    private static HttpRequest CreateRequest(string pathAndQuery, Guid familyId)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = pathAndQuery.Split('?', 2)[0];
        if (pathAndQuery.Contains('?', StringComparison.Ordinal))
        {
            context.Request.QueryString = new QueryString($"?{pathAndQuery.Split('?', 2)[1]}");
        }

        ApiResults.EnsureCorrelationId(context);
        context.Features.Set(new KinHubAuthorizationFeature(new DA.KinHub.Domain.Identity.ExternalIdentity("https://issuer", Guid.NewGuid()), familyId, Guid.NewGuid()));
        return context.Request;
    }

    private sealed class StubCatalogService(KinHubServiceCatalogResult result) : IKinHubServiceCatalogService
    {
        public Task<KinHubServiceCatalogResult> GetCatalogAsync(Guid familyId, string? language, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class AllowAccessService : IKinHubServiceAccessService
    {
        public Task EnsureAccessAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class DenyAccessService : IKinHubServiceAccessService
    {
        public Task EnsureAccessAsync(Guid familyId, string serviceKey, CancellationToken cancellationToken)
            => throw new BusinessAccessDeniedException(BusinessErrorCodes.ServiceAccessDenied, "Access is not allowed.");
    }
}
