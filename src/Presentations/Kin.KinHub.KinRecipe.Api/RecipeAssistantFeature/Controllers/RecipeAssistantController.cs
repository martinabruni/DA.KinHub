using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.KinRecipe.Api.RecipeAssistantFeature;

[ApiController]
[Route("api/recipe-assistant")]
[Authorize]
public sealed class RecipeAssistantController : ControllerBase
{
    private readonly IRecipeAssistantManager _recipeAiService;
    private readonly IRequestValidator<SuggestRecipesRequest> _suggestValidator;
    private readonly IRequestValidator<ParseRecipeRequest> _parseValidator;
    private readonly IRequestValidator<AdaptRecipeRequest> _adaptValidator;
    private readonly ICurrentUser _currentUser;

    public RecipeAssistantController(
        IRecipeAssistantManager recipeAiService,
        IRequestValidator<SuggestRecipesRequest> suggestValidator,
        IRequestValidator<ParseRecipeRequest> parseValidator,
        IRequestValidator<AdaptRecipeRequest> adaptValidator,
        ICurrentUser currentUser)
    {
        _recipeAiService = recipeAiService;
        _suggestValidator = suggestValidator;
        _parseValidator = parseValidator;
        _adaptValidator = adaptValidator;
        _currentUser = currentUser;
    }

    [HttpPost("suggest")]
    public async Task<IActionResult> SuggestAsync(
        [FromBody] SuggestRecipesRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _suggestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeAiService.SuggestRecipesAsync(request.FridgeId, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPost("parse")]
    public async Task<IActionResult> ParseAsync(
        [FromBody] ParseRecipeRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _parseValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeAiService.ParseRecipeAsync(request.RawText, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPost("adapt")]
    public async Task<IActionResult> AdaptAsync(
        [FromBody] AdaptRecipeRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _adaptValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeAiService.AdaptRecipeAsync(request.RecipeId, request.Constraints, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }
}
