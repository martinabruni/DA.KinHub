namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class CreateRecipeStepHandler : ICreateRecipeStepHandler
{
    private readonly IRecipeStepRepository _recipeStepRepository;
    private readonly IRecipeAccessService _recipeAccessService;
    private readonly IRecipeStepResponseMapper _recipeStepResponseMapper;

    public CreateRecipeStepHandler(
        IRecipeStepRepository recipeStepRepository,
        IRecipeAccessService recipeAccessService,
        IRecipeStepResponseMapper recipeStepResponseMapper)
    {
        _recipeStepRepository = recipeStepRepository;
        _recipeAccessService = recipeAccessService;
        _recipeStepResponseMapper = recipeStepResponseMapper;
    }

    public async Task<Result<RecipeStepResponse>> HandleAsync(
        CreateRecipeStepRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeAccessService.GetAccessibleRecipeAsync(request.RecipeId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<RecipeStepResponse>();
        }

        var now = DateTime.UtcNow;
        var recipeStep = new RecipeStep
        {
            Id = Guid.NewGuid(),
            Order = request.Order,
            Description = request.Description,
            RecipeId = request.RecipeId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var createdRecipeStep = await _recipeStepRepository.AddAsync(recipeStep, cancellationToken);
        return Result<RecipeStepResponse>.Success(_recipeStepResponseMapper.Map(createdRecipeStep));
    }
}
