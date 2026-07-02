using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.KinRecipe.Api.RecipeFeature;

[ApiController]
[Route("api/recipe-books/{recipeBookId:guid}/recipes")]
public sealed class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly IFridgeService _fridgeService;
    private readonly IRecipeMissingIngredientsService _missingIngredientsService;
    private readonly IRequestValidator<CreateRecipeRequest> _createValidator;
    private readonly IRequestValidator<UpdateRecipeRequest> _updateValidator;
    private readonly ICurrentUser _currentUser;

    public RecipeController(
        IRecipeService recipeService,
        IFridgeService fridgeService,
        IRecipeMissingIngredientsService missingIngredientsService,
        IRequestValidator<CreateRecipeRequest> createValidator,
        IRequestValidator<UpdateRecipeRequest> updateValidator,
        ICurrentUser currentUser)
    {
        _recipeService = recipeService;
        _fridgeService = fridgeService;
        _missingIngredientsService = missingIngredientsService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        Guid recipeBookId,
        [FromBody] CreateRecipeRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToCreatedActionResult(this, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(Guid recipeBookId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _recipeService.GetAllAsync(recipeBookId, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid recipeBookId, Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _recipeService.GetByIdAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid recipeBookId,
        Guid id,
        [FromBody] UpdateRecipeRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeService.UpdateAsync(id, request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid recipeBookId, Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _recipeService.DeleteAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPost("{id:guid}/missing-ingredients")]
    public async Task<IActionResult> GetMissingIngredientsAsync(
        Guid recipeBookId,
        Guid id,
        [FromQuery] Guid fridgeId,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var recipeResult = await _recipeService.GetByIdAsync(id, _currentUser.UserId, cancellationToken);
        if (!recipeResult.IsSuccess)
            return HttpResultMapper.ToActionResult(this, recipeResult);

        var fridgeResult = await _fridgeService.GetByIdAsync(fridgeId, _currentUser.UserId, cancellationToken);
        if (!fridgeResult.IsSuccess)
            return HttpResultMapper.ToActionResult(this, fridgeResult);

        var missing = await _missingIngredientsService.GetMissingIngredientsAsync(id, fridgeId, cancellationToken);
        return Ok(new { missingIngredients = missing });
    }
}
