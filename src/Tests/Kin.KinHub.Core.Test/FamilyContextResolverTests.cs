extern alias KinListApi;

using System.Net;
using System.Text;
using Kin.KinHub.Shared.Api.Common.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Resolver = KinListApi::Kin.KinHub.KinList.Api.Common.RemoteFamilyContextResolver;

namespace Kin.KinHub.Core.Test;

public sealed class FamilyContextResolverTests
{
    [Fact]
    public async Task ValidFamilyContext_IsResolved()
    {
        var familyId = Guid.NewGuid();
        var result = await CreateResolver(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{"familyId":"{{familyId}}"}""", Encoding.UTF8, "application/json"),
        }).ResolveAsync(Guid.NewGuid());

        Assert.Equal(FamilyContextOutcome.Success, result.Outcome);
        Assert.Equal(familyId, result.FamilyId);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task MissingFamily_IsDistinguished(HttpStatusCode status)
    {
        var result = await CreateResolver(new HttpResponseMessage(status)).ResolveAsync(Guid.NewGuid());
        Assert.Equal(FamilyContextOutcome.NoFamily, result.Outcome);
    }

    [Fact]
    public async Task MalformedFamilyContext_IsUnavailable()
    {
        var result = await CreateResolver(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"familyId":"invalid"}""", Encoding.UTF8, "application/json"),
        }).ResolveAsync(Guid.NewGuid());

        Assert.Equal(FamilyContextOutcome.Unavailable, result.Outcome);
    }

    [Fact]
    public async Task IdentityFailure_IsUnavailable()
    {
        using var client = new HttpClient(new ThrowingHttpMessageHandler(new HttpRequestException("unavailable")))
        {
            BaseAddress = new Uri("http://localhost:5001/"),
        };
        var result = CreateResolver(client).ResolveAsync(Guid.NewGuid());

        Assert.Equal(FamilyContextOutcome.Unavailable, (await result).Outcome);
    }

    private static Resolver CreateResolver(HttpResponseMessage response) =>
        CreateResolver(new HttpClient(new StubHttpMessageHandler(_ => response))
        {
            BaseAddress = new Uri("http://localhost:5001/"),
        });

    private static Resolver CreateResolver(HttpClient client)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer token";
        return new Resolver(
            client,
            new HttpContextAccessor { HttpContext = context },
            NullLogger<Resolver>.Instance);
    }
}
