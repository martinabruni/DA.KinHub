namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IUpdateRecipeIngredientHandler
{
    Task<Result<RecipeIngredientResponse>> HandleAsync(
        Guid recipeIngredientId,
        UpdateRecipeIngredientRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateRecipeIngredientHandler : IUpdateRecipeIngredientHandler
{
    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IRecipeIngredientAccessService _recipeIngredientAccessService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IRecipeIngredientResponseMapper _recipeIngredientResponseMapper;

    public UpdateRecipeIngredientHandler(
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeIngredientAccessService recipeIngredientAccessService,
        IEmbeddingService embeddingService,
        IRecipeIngredientResponseMapper recipeIngredientResponseMapper)
    {
        _recipeIngredientRepository = recipeIngredientRepository;
        _recipeIngredientAccessService = recipeIngredientAccessService;
        _embeddingService = embeddingService;
        _recipeIngredientResponseMapper = recipeIngredientResponseMapper;
    }

    public async Task<Result<RecipeIngredientResponse>> HandleAsync(
        Guid recipeIngredientId,
        UpdateRecipeIngredientRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeIngredientAccessService.GetAccessibleRecipeIngredientAsync(recipeIngredientId, userId, cancellationToken);
        if (!access.IsSuccess)
            return access.ToResult<RecipeIngredientResponse>();

        var recipeIngredient = access.RecipeIngredient!;
        var nameChanged = !recipeIngredient.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase);
        recipeIngredient.Name = request.Name;
        recipeIngredient.MeasureUnit = request.MeasureUnit;
        recipeIngredient.Quantity = request.Quantity;
        recipeIngredient.UpdatedAt = DateTime.UtcNow;

        if (nameChanged)
            recipeIngredient.Embedding = await _embeddingService.GenerateEmbeddingAsync(request.Name, cancellationToken);

        var updatedRecipeIngredient = await _recipeIngredientRepository.UpdateAsync(recipeIngredient, cancellationToken);
        return Result<RecipeIngredientResponse>.Success(_recipeIngredientResponseMapper.Map(updatedRecipeIngredient));
    }
}
