using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.KinRecipe.Api.RecipeFeature;

[ApiController]
[Route("api/recipe-books/{recipeBookId:guid}/recipes/{recipeId:guid}/steps")]
[Authorize]
public sealed class RecipeStepController : ControllerBase
{
    private readonly IRecipeStepService _recipeStepService;
    private readonly IRequestValidator<CreateRecipeStepRequest> _createValidator;
    private readonly IRequestValidator<UpdateRecipeStepRequest> _updateValidator;
    private readonly ICurrentUser _currentUser;

    public RecipeStepController(
        IRecipeStepService recipeStepService,
        IRequestValidator<CreateRecipeStepRequest> createValidator,
        IRequestValidator<UpdateRecipeStepRequest> updateValidator,
        ICurrentUser currentUser)
    {
        _recipeStepService = recipeStepService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        Guid recipeBookId,
        Guid recipeId,
        [FromBody] CreateRecipeStepRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeStepService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToCreatedActionResult(this, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(Guid recipeBookId, Guid recipeId, CancellationToken cancellationToken)
    {
        var result = await _recipeStepService.GetAllAsync(recipeId, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid recipeBookId, Guid recipeId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _recipeStepService.GetByIdAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid recipeBookId,
        Guid recipeId,
        Guid id,
        [FromBody] UpdateRecipeStepRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeStepService.UpdateAsync(id, request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid recipeBookId, Guid recipeId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _recipeStepService.DeleteAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }
}
