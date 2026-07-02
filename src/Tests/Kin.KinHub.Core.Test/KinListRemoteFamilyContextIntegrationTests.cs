extern alias KinListApi;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.KinList.Domain.KinListFeature;
using Kin.KinHub.Shared.Api.Common.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using KinListApiProgram = KinListApi::Program;
using RemoteFamilyContextResolver = KinListApi::Kin.KinHub.KinList.Api.Common.RemoteFamilyContextResolver;

namespace Kin.KinHub.Core.Test;

public sealed class KinListRemoteFamilyContextIntegrationTests
    : IClassFixture<OAuthAndAccessIntegrationTests.FamilyContextFactory>,
      IClassFixture<OAuthAndAccessIntegrationTests.NoFamilyContextFactory>
{
    private readonly OAuthApiFactory _familyFactory;
    private readonly OAuthApiFactory _noFamilyFactory;

    public KinListRemoteFamilyContextIntegrationTests(
        OAuthAndAccessIntegrationTests.FamilyContextFactory familyFactory,
        OAuthAndAccessIntegrationTests.NoFamilyContextFactory noFamilyFactory)
    {
        _familyFactory = familyFactory;
        _noFamilyFactory = noFamilyFactory;
    }

    [Fact]
    public async Task IdentityIssuedToken_CanCreateList_WhenFamilyContextExists()
    {
        using var identityClient = _familyFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using var kinListFactory = new RemoteIdentityKinListApiFactory(identityClient);
        using var kinListClient = kinListFactory.CreateClient();
        kinListClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await IssueAccessTokenAsync(identityClient));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/lists")
        {
            Content = JsonContent.Create(new
            {
                title = "Spesa",
                items = new[] { "Latte", "Pane" },
            }),
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var response = await kinListClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Spesa", body.GetProperty("title").GetString());
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task IdentityIssuedToken_WhenIdentityHasNoFamily_ReturnsFamilyRequired()
    {
        using var identityClient = _noFamilyFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        using var kinListFactory = new RemoteIdentityKinListApiFactory(identityClient);
        using var kinListClient = kinListFactory.CreateClient();
        kinListClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await IssueAccessTokenAsync(identityClient));

        var response = await kinListClient.GetAsync("/api/lists");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("family_required", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task IdentityIssuedToken_WhenIdentityIsUnavailable_FailsClosedWith503()
    {
        using var identityClient = new HttpClient(new ThrowingHttpMessageHandler(new HttpRequestException("identity unavailable")))
        {
            BaseAddress = new Uri("http://localhost:5001/"),
        };
        using var kinListFactory = new RemoteIdentityKinListApiFactory(identityClient);
        using var kinListClient = kinListFactory.CreateClient();
        kinListClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _familyFactory.CreateAccessToken());

        var response = await kinListClient.GetAsync("/api/lists");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("family_context_unavailable", body.GetProperty("code").GetString());
    }

    private static async Task<string> IssueAccessTokenAsync(HttpClient identityClient)
    {
        var authorizeResponse = await identityClient.PostAsync(
            "/authorize",
            new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["response_type"] = "code",
                ["client_id"] = OAuthApiFactory.ClientId,
                ["redirect_uri"] = OAuthApiFactory.RedirectUri,
                ["scope"] = "kinhub.api",
                ["state"] = "integration-state",
                ["code_challenge"] = ComputeCodeChallenge("integration-verifier"),
                ["code_challenge_method"] = "S256",
                ["email"] = "integration@kinhub.dev",
                ["password"] = "Password123!",
                ["decision"] = "approve",
            }!));

        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        var code = QueryHelpers.ParseQuery(authorizeResponse.Headers.Location!.Query)["code"].ToString();

        var tokenResponse = await identityClient.PostAsync(
            "/token",
            new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = OAuthApiFactory.ClientId,
                ["code"] = code,
                ["redirect_uri"] = OAuthApiFactory.RedirectUri,
                ["code_verifier"] = "integration-verifier",
            }!));

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        var body = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("access_token").GetString()!;
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier));
        return Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(hash);
    }

    private sealed class RemoteIdentityKinListApiFactory : WebApplicationFactory<KinListApiProgram>
    {
        private const string TestJwtSecret = "integration-only-kinhub-jwt-secret-000000000001";

        private readonly HttpClient _identityClient;

        public RemoteIdentityKinListApiFactory(HttpClient identityClient)
        {
            _identityClient = identityClient;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            Environment.SetEnvironmentVariable("KINHUB_ConnectionStrings__KinHub", "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub");
            Environment.SetEnvironmentVariable("KINHUB_Jwt__Issuer", "http://localhost");
            Environment.SetEnvironmentVariable("KINHUB_Jwt__Secret", TestJwtSecret);
            Environment.SetEnvironmentVariable("KINHUB_FamilyContextApi__BaseUrl", "http://localhost:5001");

            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:KinHub"] = "Host=localhost;Database=kinhub;Username=kinhub;Password=kinhub",
                    ["Jwt:Issuer"] = "http://localhost",
                    ["Jwt:Secret"] = TestJwtSecret,
                    ["Jwt:Audience"] = "kinhub.api",
                    ["FamilyContextApi:BaseUrl"] = "http://localhost:5001",
                });
            });

            builder.UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = false;
                options.ValidateScopes = false;
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IKinListRepository>();
                services.RemoveAll<IKinListItemRepository>();
                services.RemoveAll<IIdempotencyRecordRepository>();
                services.RemoveAll<IKinListTransactionExecutor>();
                services.AddSingleton(new InMemoryKinListStore());
                services.AddSingleton<IKinListRepository>(sp => sp.GetRequiredService<InMemoryKinListStore>());
                services.AddSingleton<IKinListItemRepository>(sp => sp.GetRequiredService<InMemoryKinListStore>());
                services.AddSingleton<IIdempotencyRecordRepository>(sp => sp.GetRequiredService<InMemoryKinListStore>());
                services.AddScoped<IKinListTransactionExecutor, TestKinListTransactionExecutor>();

                services.RemoveAll<IFamilyContextResolver>();
                services.AddScoped<IFamilyContextResolver>(serviceProvider =>
                    new RemoteFamilyContextResolver(
                        _identityClient,
                        serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                        NullLogger<RemoteFamilyContextResolver>.Instance));

                services.RemoveAll<IKinListAudioDraftGenerator>();
                services.AddSingleton<IKinListAudioDraftGenerator>(new ConfigurableAudioDraftGenerator());
            });
        }
    }
}
