using Kin.KinHub.App.Functions.Common;

namespace Kin.KinHub.App.Functions.RecipeAssistantFeature;

public sealed class RecipeAssistantFunctions : FunctionsTriggerBase
{
    private readonly FunctionsAuthorizationService _authorizationService;
    private readonly IRecipeAssistantManager _service;
    private readonly IRequestValidator<SuggestRecipesRequest> _suggestValidator;
    private readonly IRequestValidator<ParseRecipeRequest> _parseValidator;
    private readonly IRequestValidator<AdaptRecipeRequest> _adaptValidator;

    public RecipeAssistantFunctions(
        FunctionsAuthorizationService authorizationService,
        IRecipeAssistantManager service,
        IRequestValidator<SuggestRecipesRequest> suggestValidator,
        IRequestValidator<ParseRecipeRequest> parseValidator,
        IRequestValidator<AdaptRecipeRequest> adaptValidator)
    {
        _authorizationService = authorizationService;
        _service = service;
        _suggestValidator = suggestValidator;
        _parseValidator = parseValidator;
        _adaptValidator = adaptValidator;
    }

    [Function(nameof(SuggestAsync))]
    public async Task<IActionResult> SuggestAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/recipe-assistant/suggest")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _suggestValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _service.SuggestRecipesAsync(body.FridgeId, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(ParseAsync))]
    public async Task<IActionResult> ParseAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/recipe-assistant/parse")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _parseValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _service.ParseRecipeAsync(body.RawText, cancellationToken);
        return ToActionResult(request, result);
    }

    [Function(nameof(AdaptAsync))]
    public async Task<IActionResult> AdaptAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/recipe-assistant/adapt")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var failure = await _authorizationService.EnsureAuthenticatedAsync(request, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var (body, error) = await ReadAndValidateAsync(request, _adaptValidator, cancellationToken);
        if (error is not null || body is null)
        {
            return error!;
        }

        var result = await _service.AdaptRecipeAsync(body.RecipeId, body.Constraints, _authorizationService.CurrentUser.UserId, cancellationToken);
        return ToActionResult(request, result);
    }
}
