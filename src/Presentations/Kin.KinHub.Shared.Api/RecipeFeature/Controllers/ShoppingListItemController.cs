using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Shared.Api.RecipeFeature;

[ApiController]
[Route("api/shopping-lists/{listId:guid}/items")]
public sealed class ShoppingListItemController : ControllerBase
{
    private readonly IShoppingListItemService _shoppingListItemService;
    private readonly IRequestValidator<CreateShoppingListItemRequest> _createValidator;
    private readonly IRequestValidator<BulkAddShoppingListItemsRequest> _bulkValidator;
    private readonly ICurrentUser _currentUser;

    public ShoppingListItemController(
        IShoppingListItemService shoppingListItemService,
        IRequestValidator<CreateShoppingListItemRequest> createValidator,
        IRequestValidator<BulkAddShoppingListItemsRequest> bulkValidator,
        ICurrentUser currentUser)
    {
        _shoppingListItemService = shoppingListItemService;
        _createValidator = createValidator;
        _bulkValidator = bulkValidator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(Guid listId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        var result = await _shoppingListItemService.GetAllByListIdAsync(listId, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddAsync(
        Guid listId,
        [FromBody] CreateShoppingListItemRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (request is null)
            return BadRequest(new { message = "Invalid request body." });

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        var result = await _shoppingListItemService.AddAsync(listId, request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToCreatedActionResult(result);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkAddAsync(
        Guid listId,
        [FromBody] BulkAddShoppingListItemsRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        if (request is null)
            return BadRequest(new { message = "Invalid request body." });

        var validation = await _bulkValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        var result = await _shoppingListItemService.BulkAddAsync(listId, request, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpPatch("{itemId:guid}/toggle")]
    public async Task<IActionResult> ToggleAsync(Guid listId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        var result = await _shoppingListItemService.ToggleCheckedAsync(listId, itemId, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(result);
    }

    // NOTE: [HttpDelete("checked")] MUST be declared before [HttpDelete("{itemId:guid}")]
    // to prevent ASP.NET Core from treating "checked" as a Guid parameter.
    [HttpDelete("checked")]
    public async Task<IActionResult> DeleteCheckedAsync(Guid listId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        var result = await _shoppingListItemService.DeleteCheckedAsync(listId, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(result);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> DeleteItemAsync(Guid listId, Guid itemId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Unauthorized(new { message = "Missing or invalid Authorization header." });

        var result = await _shoppingListItemService.DeleteAsync(listId, itemId, _currentUser.UserId, cancellationToken);
        return HttpResultMapper.ToActionResult(result);
    }
}
