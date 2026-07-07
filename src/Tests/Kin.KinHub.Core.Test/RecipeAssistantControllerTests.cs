using Kin.KinHub.Core.Business.Common;
using Kin.KinHub.KinRecipe.Business.RecipeAssistantFeature;
using Kin.KinHub.KinRecipe.Api.RecipeAssistantFeature;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Core.Test;

public sealed class RecipeAssistantControllerTests
{
    [Fact]
    public async Task ParseAsync_WhenManagerReturnsUnprocessableEntity_Returns422ProblemDetails()
    {
        var controller = CreateController(new FakeRecipeAssistantManager(
            parseResult: Result<ParsedRecipeResponse?>.UnprocessableEntity("invalid payload", "recipe_assistant_invalid_response")));

        var actionResult = await controller.ParseAsync(new ParseRecipeRequest { RawText = "pasta" }, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problem.Status);
        Assert.Equal("recipe_assistant_invalid_response", problem.Extensions["code"]);
    }

    [Fact]
    public async Task SuggestAsync_WhenManagerReturnsServiceUnavailable_Returns503ProblemDetails()
    {
        var controller = CreateController(new FakeRecipeAssistantManager(
            suggestResult: Result<SuggestRecipesResult>.ServiceUnavailable("upstream down", "recipe_assistant_unavailable")));

        var actionResult = await controller.SuggestAsync(new SuggestRecipesRequest { FridgeId = Guid.NewGuid() }, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.Status);
        Assert.Equal("recipe_assistant_unavailable", problem.Extensions["code"]);
    }

    [Fact]
    public async Task AdaptAsync_WhenManagerReturnsUnauthorized_Returns403ProblemDetails()
    {
        var controller = CreateController(new FakeRecipeAssistantManager(
            adaptResult: Result<RecipeAdaptationResponse>.Unauthorized("forbidden", "forbidden")));

        var actionResult = await controller.AdaptAsync(
            new AdaptRecipeRequest
            {
                RecipeId = Guid.NewGuid(),
                Constraints = ["vegan"],
            },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status403Forbidden, problem.Status);
        Assert.Equal("forbidden", problem.Extensions["code"]);
    }

    private static RecipeAssistantController CreateController(IRecipeAssistantManager manager)
    {
        var controller = new RecipeAssistantController(
            manager,
            new PassThroughValidator<SuggestRecipesRequest>(),
            new PassThroughValidator<ParseRecipeRequest>(),
            new PassThroughValidator<AdaptRecipeRequest>(),
            new FakeCurrentUser());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.ControllerContext.HttpContext.TraceIdentifier = "test-correlation";
        return controller;
    }

    private sealed class FakeRecipeAssistantManager : IRecipeAssistantManager
    {
        private readonly Result<SuggestRecipesResult> _suggestResult;
        private readonly Result<ParsedRecipeResponse?> _parseResult;
        private readonly Result<RecipeAdaptationResponse> _adaptResult;

        public FakeRecipeAssistantManager(
            Result<SuggestRecipesResult>? suggestResult = null,
            Result<ParsedRecipeResponse?>? parseResult = null,
            Result<RecipeAdaptationResponse>? adaptResult = null)
        {
            _suggestResult = suggestResult ?? Result<SuggestRecipesResult>.Success(new SuggestRecipesResult());
            _parseResult = parseResult ?? Result<ParsedRecipeResponse?>.Success(null);
            _adaptResult = adaptResult ?? Result<RecipeAdaptationResponse>.Success(new RecipeAdaptationResponse
            {
                OriginalRecipe = new ParsedRecipeResponse
                {
                    Name = "Original",
                    FinalTime = TimeSpan.FromMinutes(10),
                    Portions = 2,
                    Ingredients = [],
                    Steps = [],
                },
                AdaptedRecipe = new ParsedRecipeResponse
                {
                    Name = "Adapted",
                    FinalTime = TimeSpan.FromMinutes(10),
                    Portions = 2,
                    Ingredients = [],
                    Steps = [],
                },
            });
        }

        public Task<Result<SuggestRecipesResult>> SuggestRecipesAsync(Guid fridgeId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_suggestResult);

        public Task<Result<ParsedRecipeResponse?>> ParseRecipeAsync(string rawText, CancellationToken cancellationToken = default) =>
            Task.FromResult(_parseResult);

        public Task<Result<RecipeAdaptationResponse>> AdaptRecipeAsync(Guid recipeId, IReadOnlyList<string> constraints, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_adaptResult);
    }
}
