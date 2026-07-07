namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class CreateRecipeIngredientHandler : ICreateRecipeIngredientHandler
{
    private readonly IRecipeIngredientRepository _recipeIngredientRepository;
    private readonly IRecipeAccessService _recipeAccessService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IRecipeIngredientResponseMapper _recipeIngredientResponseMapper;

    public CreateRecipeIngredientHandler(
        IRecipeIngredientRepository recipeIngredientRepository,
        IRecipeAccessService recipeAccessService,
        IEmbeddingService embeddingService,
        IRecipeIngredientResponseMapper recipeIngredientResponseMapper)
    {
        _recipeIngredientRepository = recipeIngredientRepository;
        _recipeAccessService = recipeAccessService;
        _embeddingService = embeddingService;
        _recipeIngredientResponseMapper = recipeIngredientResponseMapper;
    }

    public async Task<Result<RecipeIngredientResponse>> HandleAsync(
        CreateRecipeIngredientRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _recipeAccessService.GetAccessibleRecipeAsync(request.RecipeId, userId, cancellationToken);
        if (!access.IsSuccess)
        {
            return access.ToResult<RecipeIngredientResponse>();
        }

        var now = DateTime.UtcNow;
        var recipeIngredient = new RecipeIngredient
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            MeasureUnit = request.MeasureUnit,
            Quantity = request.Quantity,
            RecipeId = request.RecipeId,
            Embedding = await _embeddingService.GenerateEmbeddingAsync(request.Name, cancellationToken),
            CreatedAt = now,
            UpdatedAt = now,
        };

        var createdRecipeIngredient = await _recipeIngredientRepository.AddAsync(recipeIngredient, cancellationToken);
        return Result<RecipeIngredientResponse>.Success(_recipeIngredientResponseMapper.Map(createdRecipeIngredient));
    }
}
