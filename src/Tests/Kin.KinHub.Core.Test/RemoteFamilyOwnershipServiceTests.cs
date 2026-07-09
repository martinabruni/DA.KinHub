using System.Net;
using System.Net.Http;
using System.Text;
using Kin.KinHub.App.Functions.Common;
using Kin.KinHub.Core.Business.Common;
using Kin.KinHub.Core.Business.FamilyFeature;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kin.KinHub.Core.Test;

public sealed class RemoteFamilyOwnershipServiceTests
{
    [Fact]
    public async Task GetCurrentFamilyAsync_WhenCoreReturnsFamilyContext_ReturnsResolvedFamily()
    {
        var familyId = Guid.Parse("93cc2b26-f0af-43ce-a79b-3ed2373200ab");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"familyId":"{{familyId}}"}""", Encoding.UTF8, "application/json"),
            }))
        {
            BaseAddress = new Uri("http://localhost:5000/"),
        };

        var service = CreateService(httpClient);

        var result = await service.GetCurrentFamilyAsync(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(familyId, result.Family!.Id);
    }

    [Fact]
    public async Task GetCurrentFamilyAsync_WhenCoreIsUnavailable_FailsClosed()
    {
        using var httpClient = new HttpClient(new ThrowingHttpMessageHandler(new HttpRequestException("boom")))
        {
            BaseAddress = new Uri("http://localhost:5000/"),
        };

        var service = CreateService(httpClient);

        var result = await service.GetCurrentFamilyAsync(Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ServiceUnavailable, result.Status);
    }

    [Fact]
    public async Task EnsureOwnershipAsync_WhenFamilyDiffers_ReturnsUnauthorized()
    {
        var familyId = Guid.Parse("3b8f33fe-a7d4-4ea3-bf1a-86b308779ec7");
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"familyId":"{{familyId}}"}""", Encoding.UTF8, "application/json"),
            }))
        {
            BaseAddress = new Uri("http://localhost:5000/"),
        };

        var service = CreateService(httpClient);

        var result = await service.EnsureOwnershipAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    private static RemoteFamilyOwnershipService CreateService(HttpClient httpClient)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer test-token";

        return new RemoteFamilyOwnershipService(
            httpClient,
            new HttpContextAccessor { HttpContext = httpContext },
            NullLogger<RemoteFamilyOwnershipService>.Instance);
    }
}

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(_responseFactory(request));
}

internal sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    private readonly Exception _exception;

    public ThrowingHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromException<HttpResponseMessage>(_exception);
}
