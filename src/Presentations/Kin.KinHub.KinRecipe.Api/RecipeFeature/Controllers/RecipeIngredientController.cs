using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.KinRecipe.Api.RecipeFeature;

[ApiController]
[Route("api/recipe-books/{recipeBookId:guid}/recipes/{recipeId:guid}/ingredients")]
[Authorize]
public sealed class RecipeIngredientController : ControllerBase
{
    private readonly IRecipeIngredientService _recipeIngredientService;
    private readonly IRequestValidator<CreateRecipeIngredientRequest> _createValidator;
    private readonly IRequestValidator<UpdateRecipeIngredientRequest> _updateValidator;
    private readonly ICurrentUser _currentUser;

    public RecipeIngredientController(
        IRecipeIngredientService recipeIngredientService,
        IRequestValidator<CreateRecipeIngredientRequest> createValidator,
        IRequestValidator<UpdateRecipeIngredientRequest> updateValidator,
        ICurrentUser currentUser)
    {
        _recipeIngredientService = recipeIngredientService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        Guid recipeBookId,
        Guid recipeId,
        [FromBody] CreateRecipeIngredientRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeIngredientService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToCreatedActionResult(this, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(Guid recipeBookId, Guid recipeId, CancellationToken cancellationToken)
    {
        var result = await _recipeIngredientService.GetAllAsync(recipeId, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid recipeBookId, Guid recipeId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _recipeIngredientService.GetByIdAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid recipeBookId,
        Guid recipeId,
        Guid id,
        [FromBody] UpdateRecipeIngredientRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeIngredientService.UpdateAsync(id, request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid recipeBookId, Guid recipeId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _recipeIngredientService.DeleteAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }
}
