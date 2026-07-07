using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class KinHubFridgeIngredientService : IFridgeIngredientService
{
    private readonly IFridgeIngredientRepository _fridgeIngredientRepository;
    private readonly IFridgeRepository _fridgeRepository;
    private readonly IFamilyRepository _familyRepository;
    private readonly IEmbeddingService _embeddingService;

    public KinHubFridgeIngredientService(
        IFridgeIngredientRepository fridgeIngredientRepository,
        IFridgeRepository fridgeRepository,
        IFamilyRepository familyRepository,
        IEmbeddingService embeddingService)
    {
        _fridgeIngredientRepository = fridgeIngredientRepository;
        _fridgeRepository = fridgeRepository;
        _familyRepository = familyRepository;
        _embeddingService = embeddingService;
    }

    public async Task<Result<FridgeIngredientResponse>> CreateAsync(
        CreateFridgeIngredientRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<FridgeIngredientResponse>.NotFound("Family not found for the current user.");

        var fridge = await _fridgeRepository.GetByIdAsync(request.FridgeId, cancellationToken);
        if (fridge is null)
            return Result<FridgeIngredientResponse>.NotFound("Fridge not found.");
        if (fridge.FamilyId != family.Id)
            return Result<FridgeIngredientResponse>.Unauthorized("Access denied.");

        var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Name, cancellationToken);

        var now = DateTime.UtcNow;
        var ingredient = new FridgeIngredient
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            MeasureUnit = request.MeasureUnit,
            Quantity = request.Quantity,
            FridgeId = request.FridgeId,
            Embedding = embedding,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _fridgeIngredientRepository.AddAsync(ingredient, cancellationToken);
        return Result<FridgeIngredientResponse>.Success(Map(created));
    }

    public async Task<Result<IReadOnlyList<FridgeIngredientResponse>>> GetAllAsync(
        Guid fridgeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<IReadOnlyList<FridgeIngredientResponse>>.NotFound("Family not found for the current user.");

        var fridge = await _fridgeRepository.GetByIdAsync(fridgeId, cancellationToken);
        if (fridge is null)
            return Result<IReadOnlyList<FridgeIngredientResponse>>.NotFound("Fridge not found.");
        if (fridge.FamilyId != family.Id)
            return Result<IReadOnlyList<FridgeIngredientResponse>>.Unauthorized("Access denied.");

        var ingredients = await _fridgeIngredientRepository.GetAllByFridgeIdAsync(fridgeId, cancellationToken);
        return Result<IReadOnlyList<FridgeIngredientResponse>>.Success(ingredients.Select(Map).ToList());
    }

    public async Task<Result<FridgeIngredientResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<FridgeIngredientResponse>.NotFound("Family not found for the current user.");

        var ingredient = await _fridgeIngredientRepository.GetByIdAsync(id, cancellationToken);
        if (ingredient is null)
            return Result<FridgeIngredientResponse>.NotFound("Fridge ingredient not found.");

        var fridge = await _fridgeRepository.GetByIdAsync(ingredient.FridgeId, cancellationToken);
        if (fridge is null || fridge.FamilyId != family.Id)
            return Result<FridgeIngredientResponse>.Unauthorized("Access denied.");

        return Result<FridgeIngredientResponse>.Success(Map(ingredient));
    }

    public async Task<Result<FridgeIngredientResponse>> UpdateAsync(
        Guid id,
        UpdateFridgeIngredientRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<FridgeIngredientResponse>.NotFound("Family not found for the current user.");

        var ingredient = await _fridgeIngredientRepository.GetByIdAsync(id, cancellationToken);
        if (ingredient is null)
            return Result<FridgeIngredientResponse>.NotFound("Fridge ingredient not found.");

        var fridge = await _fridgeRepository.GetByIdAsync(ingredient.FridgeId, cancellationToken);
        if (fridge is null || fridge.FamilyId != family.Id)
            return Result<FridgeIngredientResponse>.Unauthorized("Access denied.");

        var nameChanged = !ingredient.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase);
        ingredient.Name = request.Name;
        ingredient.MeasureUnit = request.MeasureUnit;
        ingredient.Quantity = request.Quantity;
        ingredient.UpdatedAt = DateTime.UtcNow;

        if (nameChanged)
            ingredient.Embedding = await _embeddingService.GenerateEmbeddingAsync(request.Name, cancellationToken);

        var updated = await _fridgeIngredientRepository.UpdateAsync(ingredient, cancellationToken);
        return Result<FridgeIngredientResponse>.Success(Map(updated));
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<bool>.NotFound("Family not found for the current user.");

        var ingredient = await _fridgeIngredientRepository.GetByIdAsync(id, cancellationToken);
        if (ingredient is null)
            return Result<bool>.NotFound("Fridge ingredient not found.");

        var fridge = await _fridgeRepository.GetByIdAsync(ingredient.FridgeId, cancellationToken);
        if (fridge is null || fridge.FamilyId != family.Id)
            return Result<bool>.Unauthorized("Access denied.");

        await _fridgeIngredientRepository.SoftDeleteAsync(id, cancellationToken);
        return Result<bool>.Success(true);
    }

    private static FridgeIngredientResponse Map(FridgeIngredient i) =>
        new() { Id = i.Id, Name = i.Name, MeasureUnit = i.MeasureUnit, Quantity = i.Quantity, FridgeId = i.FridgeId };
}
