using Kin.KinHub.Core.Business.Common;
using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Identity.Domain.AuthenticationFeature;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kin.KinHub.Core.Test;

public sealed class McpIntegrationTests : IClassFixture<McpApiFactory>
{
    private readonly McpApiFactory _factory;

    public McpIntegrationTests(McpApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Initialize_ReturnsSessionHeaderAndCapabilities()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new
                {
                    name = "test-client",
                    version = "1.0.0",
                },
            },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Mcp-Session-Id", out var values));
        Assert.False(string.IsNullOrWhiteSpace(values.Single()));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2025-03-26", body.GetProperty("result").GetProperty("protocolVersion").GetString());
        Assert.True(body.GetProperty("result").GetProperty("capabilities").GetProperty("tools").GetProperty("listChanged").ValueKind is JsonValueKind.False);
    }

    [Fact]
    public async Task ToolsList_AfterInitialization_ReturnsToolCatalog()
    {
        using var client = _factory.CreateClient();
        var sessionId = await InitializeSessionAsync(client);

        var ready = await client.PostAsJsonAsync("/api/v1/mcp", new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized",
        }, sessionId);
        Assert.Equal(HttpStatusCode.Accepted, ready.StatusCode);

        var response = await client.PostAsJsonAsync("/api/v1/mcp", new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { },
        }, sessionId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var toolNames = body.GetProperty("result").GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .ToArray();

        Assert.Contains("auth.login", toolNames);
        Assert.Contains("family.manage", toolNames);
        Assert.Contains("recipe-assistant.parse", toolNames);
    }

    [Fact]
    public async Task FamilyManage_Get_ReturnsToolPayloadWhenAuthenticated()
    {
        using var client = _factory.CreateClient();
        var sessionId = await InitializeSessionAsync(client);

        var initialized = await client.PostAsJsonAsync("/api/v1/mcp", new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized",
        }, sessionId);
        Assert.Equal(HttpStatusCode.Accepted, initialized.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateAccessToken());

        var response = await client.PostAsJsonAsync("/api/v1/mcp", new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "family.manage",
                arguments = new
                {
                    action = "get",
                },
            },
        }, sessionId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var toolResult = body.GetProperty("result");
        Assert.False(toolResult.GetProperty("isError").GetBoolean());

        var payload = JsonDocument.Parse(toolResult.GetProperty("content")[0].GetProperty("text").GetString()!);
        Assert.Equal("Kin Family", payload.RootElement.GetProperty("name").GetString());
    }

    private static async Task<string> InitializeSessionAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/mcp", new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new
                {
                    name = "integration-test",
                    version = "1.0.0",
                },
            },
        });

        response.EnsureSuccessStatusCode();
        return response.Headers.GetValues("Mcp-Session-Id").Single();
    }
}

public sealed class McpApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KinHub"] = "Host=localhost;Database=kinhub;Username=test;Password=test",
                ["Jwt:Secret"] = "abcdefghijklmnopqrstuvwxyz123456",
                ["Jwt:Issuer"] = "kinhub-tests",
                ["OpenAi:Endpoint"] = "https://localhost/",
                ["OpenAi:ApiKey"] = "test-key",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFamilyService>();
            services.AddScoped<IFamilyService, FakeFamilyService>();
        });
    }

    public string CreateAccessToken()
    {
        using var scope = Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();
        return tokenGenerator.GenerateAccessToken(
            new KinUser
            {
                Id = Guid.Parse("08cbaf25-9470-4c33-8542-9a81151ffb26"),
                Email = "member@kinhub.dev",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            []);
    }
}

internal static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostAsJsonAsync(
        this HttpClient client,
        string requestUri,
        object payload,
        string? sessionId = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            request.Headers.Add("Mcp-Session-Id", sessionId);
        }

        return await client.SendAsync(request);
    }
}

internal sealed class FakeFamilyService : IFamilyService
{
    public Task<Result<AddFamilyMemberResponse>> AddFamilyMemberAsync(Guid familyId, AddFamilyMemberRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<CreateFamilyResponse>> CreateFamilyAsync(CreateFamilyRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<bool>> DeleteFamilyAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<bool>> DeleteFamilyMemberAsync(Guid familyId, Guid memberId, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<FamilyDetailResponse>> GetFamilyAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<FamilyDetailResponse>.Success(new FamilyDetailResponse
        {
            Id = Guid.Parse("1f440eb4-1b6d-4e0e-9eb2-0bdb4a3ef624"),
            Name = "Kin Family",
            Members =
            [
                new FamilyMemberDto
                {
                    Id = Guid.Parse("3a3e6553-429d-4796-bc90-dd4106d80e61"),
                    Name = "Martina",
                },
            ],
        }));

    public Task<Result<UpdateFamilyResponse>> UpdateFamilyAsync(Guid familyId, UpdateFamilyRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Result<UpdateFamilyMemberResponse>> UpdateFamilyMemberAsync(Guid familyId, Guid memberId, UpdateFamilyMemberRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
