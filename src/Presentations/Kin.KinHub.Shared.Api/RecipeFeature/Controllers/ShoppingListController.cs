using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.RecipeFeature;

[ApiController]
[Route("api/shopping-lists")]
public sealed class ShoppingListController : ControllerBase
{
    private readonly IShoppingListService _shoppingListService;
    private readonly IRequestValidator<CreateShoppingListRequest> _createValidator;
    private readonly IRequestValidator<UpdateShoppingListRequest> _updateValidator;
    private readonly ICurrentUser _currentUser;

    public ShoppingListController(
        IShoppingListService shoppingListService,
        IRequestValidator<CreateShoppingListRequest> createValidator,
        IRequestValidator<UpdateShoppingListRequest> updateValidator,
        ICurrentUser currentUser)
    {
        _shoppingListService = shoppingListService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _shoppingListService.GetAllAsync(_currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _shoppingListService.GetByIdAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateShoppingListRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _shoppingListService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToCreatedActionResult(this, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateShoppingListRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        if (request is null)
            return ApiProblemDetails.InvalidRequestBody(this);

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiProblemDetails.Validation(this, validation.Errors);

        var result = await _shoppingListService.UpdateAsync(id, request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return ApiProblemDetails.AuthenticationRequired(this);

        var result = await _shoppingListService.DeleteAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(this, result);
    }
}
