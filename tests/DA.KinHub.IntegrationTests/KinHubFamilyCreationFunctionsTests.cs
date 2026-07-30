using System.Text;
using System.Text.Json;
using DA.KinHub.Business.Common;
using DA.KinHub.Business.Identity;
using DA.KinHub.Domain.Identity;
using DA.KinHub.Functions.Configuration;
using DA.KinHub.Functions.Functions;
using DA.KinHub.Functions.Http;
using DA.KinHub.Functions.Observability;
using DA.KinHub.Functions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DA.KinHub.IntegrationTests;

public sealed class KinHubFamilyCreationFunctionsTests
{
    [Fact]
    public async Task CreateFamilyReturns201WhenCreated()
    {
        var familyId = Guid.NewGuid();
        var functions = new KinHubFamilyCreationFunctions(
            new StubFamilyCreationService(FamilyCreationResult.CreatedFamily(familyId)),
            CreateTelemetry());
        var request = CreateRequest(new { name = "Famiglia Bruni" });

        var result = await functions.CreateFamily(request, CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
        var payload = Assert.IsType<KinHubBootstrapResult>(response.Value);
        Assert.Equal("family", payload.State);
        Assert.Equal(familyId, payload.FamilyId);
    }

    [Fact]
    public async Task CreateFamilyReturns200WhenExisting()
    {
        var familyId = Guid.NewGuid();
        var functions = new KinHubFamilyCreationFunctions(
            new StubFamilyCreationService(FamilyCreationResult.ExistingFamily(familyId, reconciledConflict: false)),
            CreateTelemetry());
        var request = CreateRequest(new { name = "Famiglia Bruni" });

        var result = await functions.CreateFamily(request, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<KinHubBootstrapResult>(response.Value);
        Assert.Equal("family", payload.State);
        Assert.Equal(familyId, payload.FamilyId);
    }

    [Fact]
    public async Task CreateFamilyRejectsMissingJsonBody()
    {
        var functions = new KinHubFamilyCreationFunctions(
            new StubFamilyCreationService(FamilyCreationResult.CreatedFamily(Guid.NewGuid())),
            CreateTelemetry());
        var request = CreateRequest(string.Empty);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => functions.CreateFamily(request, CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.FamilyNameInvalid, exception.Code);
    }

    [Fact]
    public async Task CreateFamilyRejectsMalformedJson()
    {
        var functions = new KinHubFamilyCreationFunctions(
            new StubFamilyCreationService(FamilyCreationResult.CreatedFamily(Guid.NewGuid())),
            CreateTelemetry());
        var request = CreateRequest("{ bad json }");

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => functions.CreateFamily(request, CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.FamilyNameInvalid, exception.Code);
    }

    [Fact]
    public async Task CreateFamilyPropagatesDependencyFailure()
    {
        var functions = new KinHubFamilyCreationFunctions(
            new ThrowingFamilyCreationService(new BusinessDependencyException(BusinessErrorCodes.PostgreSqlUnavailable, "db unavailable")),
            CreateTelemetry());
        var request = CreateRequest(new { name = "Famiglia Bruni" });

        var exception = await Assert.ThrowsAsync<BusinessDependencyException>(() => functions.CreateFamily(request, CancellationToken.None));

        Assert.Equal(BusinessErrorCodes.PostgreSqlUnavailable, exception.Code);
    }

    private static KinHubTelemetry CreateTelemetry()
        => new(new BuildInfoProvider(Options.Create(new RuntimeOptions { AppName = "KinHub", ApiVersion = "1.0", Environment = "Test" })));

    private static HttpRequest CreateRequest(object? body)
    {
        return body switch
        {
            null => CreateRequest(string.Empty),
            string raw => CreateRequest(raw),
            _ => CreateRequest(JsonSerializer.Serialize(body))
        };
    }

    private static HttpRequest CreateRequest(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/kinhub/families";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        ApiResults.EnsureCorrelationId(context);
        context.Features.Set(new KinHubAuthorizationFeature(new ExternalIdentity("https://issuer", Guid.NewGuid()), null, null));
        return context.Request;
    }

    private sealed class StubFamilyCreationService(FamilyCreationResult result) : IFamilyCreationService
    {
        public Task<FamilyCreationResult> CreateFamilyAsync(ExternalIdentity externalIdentity, string? name, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class ThrowingFamilyCreationService(Exception exception) : IFamilyCreationService
    {
        public Task<FamilyCreationResult> CreateFamilyAsync(ExternalIdentity externalIdentity, string? name, CancellationToken cancellationToken)
            => Task.FromException<FamilyCreationResult>(exception);
    }
}
