using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.RecipeFeature;

[ApiController]
[Route("api/recipe-books")]
public sealed class RecipeBookController : ControllerBase
{
    private readonly IRecipeBookService _recipeBookService;
    private readonly IRequestValidator<CreateRecipeBookRequest> _createValidator;
    private readonly IRequestValidator<UpdateRecipeBookRequest> _updateValidator;
    private readonly ICurrentUser _currentUser;

    public RecipeBookController(
        IRecipeBookService recipeBookService,
        IRequestValidator<CreateRecipeBookRequest> createValidator,
        IRequestValidator<UpdateRecipeBookRequest> updateValidator,
        ICurrentUser currentUser)
    {
        _recipeBookService = recipeBookService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateRecipeBookRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeBookService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToCreatedActionResult(this, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _recipeBookService.GetAllAsync(_currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _recipeBookService.GetByIdAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateRecipeBookRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _recipeBookService.UpdateAsync(id, request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _recipeBookService.DeleteAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }
}
