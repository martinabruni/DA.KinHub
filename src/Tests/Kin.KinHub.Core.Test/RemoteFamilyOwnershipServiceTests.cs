extern alias KinRecipeApi;

using System.Net;
using System.Net.Http;
using System.Text;
using Kin.KinHub.Core.Business.Common;
using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.Core.Business.RecipeFeature;
using Kin.KinHub.Identity.Domain.Common;
using Kin.KinHub.Shared.Api.Common;
using Kin.KinHub.Shared.Api.RecipeFeature;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using RemoteFamilyOwnershipService = KinRecipeApi::Kin.KinHub.KinRecipe.Api.Common.RemoteFamilyOwnershipService;

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

    [Fact]
    public async Task RecipeBookController_WhenServiceUnavailable_ReturnsProblemDetails503()
    {
        var controller = new RecipeBookController(
            new ServiceUnavailableRecipeBookService(),
            new PassThroughValidator<CreateRecipeBookRequest>(),
            new PassThroughValidator<UpdateRecipeBookRequest>(),
            new FakeCurrentUser());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        var response = await controller.GetAllAsync(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("service_unavailable", problem.Extensions["code"]);
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

internal sealed class ServiceUnavailableRecipeBookService : IRecipeBookService
{
    public Task<Result<RecipeBookResponse>> CreateAsync(CreateRecipeBookRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<RecipeBookResponse>.ServiceUnavailable("Core is unavailable."));

    public Task<Result<IReadOnlyList<RecipeBookResponse>>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<IReadOnlyList<RecipeBookResponse>>.ServiceUnavailable("Core is unavailable."));

    public Task<Result<RecipeBookResponse>> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<RecipeBookResponse>.ServiceUnavailable("Core is unavailable."));

    public Task<Result<RecipeBookResponse>> UpdateAsync(Guid id, UpdateRecipeBookRequest request, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<RecipeBookResponse>.ServiceUnavailable("Core is unavailable."));

    public Task<Result<bool>> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<bool>.ServiceUnavailable("Core is unavailable."));
}

internal sealed class PassThroughValidator<T> : IRequestValidator<T>
{
    public Task<RequestValidationResult> ValidateAsync(T request, CancellationToken cancellationToken = default) =>
        Task.FromResult(RequestValidationResult.Success());
}

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid UserId { get; } = Guid.NewGuid();
    public string Email { get; } = "test@kinhub.dev";
    public IReadOnlyList<string> Roles { get; } = [];
    public bool IsAuthenticated { get; } = true;
    public Guid FamilyId { get; } = Guid.NewGuid();
    public bool HasFamilyContext { get; } = true;
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
