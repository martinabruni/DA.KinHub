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
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        var result = await _shoppingListService.GetAllAsync(_currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        var result = await _shoppingListService.GetByIdAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateShoppingListRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (request is null)
            return BadRequest(new { message = "Invalid request body." });

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        var result = await _shoppingListService.CreateAsync(request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToCreatedActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateShoppingListRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (request is null)
            return BadRequest(new { message = "Invalid request body." });

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        var result = await _shoppingListService.UpdateAsync(id, request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        var result = await _shoppingListService.DeleteAsync(id, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(result);
    }
}
