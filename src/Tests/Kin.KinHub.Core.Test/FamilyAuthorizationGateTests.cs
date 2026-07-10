using Kin.KinHub.App.Functions.Common;
using Kin.KinHub.App.Functions.Common.Authorization;
using Kin.KinHub.App.Functions.Common.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Kin.KinHub.Identity.Domain.AuthenticationFeature;
using Kin.KinHub.Identity.Jwt.AuthenticationFeature;

namespace Kin.KinHub.Core.Test;

public sealed class FamilyAuthorizationGateTests
{
    private static readonly Guid FamilyId = Guid.Parse("b5f1c687-3a8f-44cf-b75f-caa1f8c5b755");
    private static readonly Guid UserId = Guid.Parse("5fb90fe2-31fd-4295-a81f-421fd3e8b8d2");

    [Fact]
    public async Task RegisterWithoutFamily_FamilyContext_ReturnsForbidden()
    {
        var (service, _, currentUser, _) = CreateService(
            tokenClaims: ValidClaims(),
            familyResolution: FamilyContextResolution.NoFamily());

        var result = await service.EnsureFamilyContextAsync(CreateRequest(), CancellationToken.None);

        var problem = AssertProblem(result, StatusCodes.Status403Forbidden, "family_required");
        Assert.Equal("The authenticated user does not currently belong to a family.", problem.Detail);
        Assert.False(currentUser.HasFamilyContext);
    }

    [Fact]
    public async Task CreateFamily_ThenFamilyContext_UsesSameTokenNoReissue()
    {
        var (service, _, currentUser, familyResolver) = CreateService(
            tokenClaims: ValidClaims(),
            familyResolution: FamilyContextResolution.Success(FamilyId));

        var firstResult = await service.EnsureFamilyContextAsync(CreateRequest(), CancellationToken.None);

        Assert.Null(firstResult);
        Assert.True(currentUser.HasFamilyContext);
        Assert.Equal(FamilyId, currentUser.FamilyId);

        familyResolver.Result = FamilyContextResolution.Success(FamilyId);
        var secondResult = await service.EnsureFamilyContextAsync(CreateRequest(), CancellationToken.None);

        Assert.Null(secondResult);
        Assert.Equal(FamilyId, currentUser.FamilyId);
    }

    [Fact]
    public async Task LeaveFamily_NextRequestImmediatelyForbidden_NoCache()
    {
        var (service, _, _, familyResolver) = CreateService(
            tokenClaims: ValidClaims(),
            familyResolution: FamilyContextResolution.Success(FamilyId));

        var first = await service.EnsureFamilyContextAsync(CreateRequest(), CancellationToken.None);
        Assert.Null(first);

        familyResolver.Result = FamilyContextResolution.NoFamily();
        var second = await service.EnsureFamilyContextAsync(CreateRequest(), CancellationToken.None);

        AssertProblem(second, StatusCodes.Status403Forbidden, "family_required");
    }

    [Fact]
    public async Task CoreUnavailable_FamilyEndpoint_FailsClosedWith503()
    {
        var (service, _, _, _) = CreateService(
            tokenClaims: ValidClaims(),
            familyResolution: FamilyContextResolution.Unavailable());

        var result = await service.EnsureFamilyContextAsync(CreateRequest(), CancellationToken.None);

        var problem = AssertProblem(result, StatusCodes.Status503ServiceUnavailable, "family_context_unavailable");
        Assert.Equal("Family context could not be resolved because Identity is unavailable.", problem.Detail);
    }

    [Fact]
    public async Task RegisterWithoutToken_ReturnsUnauthorized()
    {
        var (service, _, _, _) = CreateService(
            tokenClaims: ValidClaims(),
            familyResolution: FamilyContextResolution.Success(FamilyId));

        var result = await service.EnsureFamilyContextAsync(new DefaultHttpContext().Request, CancellationToken.None);

        AssertProblem(result, StatusCodes.Status401Unauthorized, "authentication_required");
    }

    [Fact]
    public async Task RegisterWithoutReadScope_ReturnsUnauthorized()
    {
        var (service, _, _, _) = CreateService(
            tokenClaims: ValidClaims(scopes: []),
            familyResolution: FamilyContextResolution.Success(FamilyId));

        var result = await service.EnsureFamilyContextAsync(CreateRequest(), CancellationToken.None);

        AssertProblem(result, StatusCodes.Status401Unauthorized, "authentication_required");
    }

    [Fact]
    public async Task RegisterWithInvalidToken_ReturnsUnauthorized()
    {
        var (service, _, _, _) = CreateService(
            tokenClaims: null,
            familyResolution: FamilyContextResolution.Success(FamilyId));

        var result = await service.EnsureFamilyContextAsync(CreateRequest(), CancellationToken.None);

        AssertProblem(result, StatusCodes.Status401Unauthorized, "authentication_required");
    }

    private static (FunctionsAuthorizationService service, StubTokenValidator tokenValidator, CurrentUser currentUser, StubFamilyContextResolver familyResolver) CreateService(TokenClaims? tokenClaims, FamilyContextResolution familyResolution)
    {
        var tokenValidator = new StubTokenValidator { Result = tokenClaims };
        var currentUser = new CurrentUser();
        var familyResolver = new StubFamilyContextResolver { Result = familyResolution };
        var service = new FunctionsAuthorizationService(tokenValidator, currentUser, familyResolver);
        return (service, tokenValidator, currentUser, familyResolver);
    }

    private static TokenClaims ValidClaims(IReadOnlyList<string>? scopes = null) =>
        new(UserId, "integration@kinhub.dev", [], scopes ?? [OAuthScopes.Read]);

    private static HttpRequest CreateRequest()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/access/family-context";
        context.Request.Headers.Authorization = "Bearer integration-token";
        return context.Request;
    }

    private static ProblemDetails AssertProblem(IActionResult? result, int statusCode, string code)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(statusCode, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(code, problem.Extensions["code"]);
        Assert.False(string.IsNullOrWhiteSpace(problem.Extensions["correlationId"]?.ToString()));
        return problem;
    }

    private sealed class StubTokenValidator : ITokenValidator
    {
        public TokenClaims? Result { get; set; }

        public TokenClaims? ValidateAccessToken(string token) => Result;
    }

    private sealed class StubFamilyContextResolver : IFamilyContextResolver
    {
        public FamilyContextResolution Result { get; set; } = FamilyContextResolution.NoFamily();

        public Task<FamilyContextResolution> ResolveAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result);
    }
}
