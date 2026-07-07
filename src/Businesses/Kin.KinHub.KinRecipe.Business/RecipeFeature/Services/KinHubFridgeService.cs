using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class KinHubFridgeService : IFridgeService
{
    private readonly IFridgeRepository _fridgeRepository;
    private readonly IFamilyRepository _familyRepository;

    public KinHubFridgeService(IFridgeRepository fridgeRepository, IFamilyRepository familyRepository)
    {
        _fridgeRepository = fridgeRepository;
        _familyRepository = familyRepository;
    }

    public async Task<Result<FridgeResponse>> CreateAsync(
        CreateFridgeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<FridgeResponse>.NotFound("Family not found for the current user.");

        var now = DateTime.UtcNow;
        var fridge = new Fridge
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            FamilyId = family.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _fridgeRepository.AddAsync(fridge, cancellationToken);
        return Result<FridgeResponse>.Success(Map(created));
    }

    public async Task<Result<IReadOnlyList<FridgeResponse>>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<IReadOnlyList<FridgeResponse>>.NotFound("Family not found for the current user.");

        var fridges = await _fridgeRepository.GetAllByFamilyIdAsync(family.Id, cancellationToken);
        return Result<IReadOnlyList<FridgeResponse>>.Success(fridges.Select(Map).ToList());
    }

    public async Task<Result<FridgeResponse>> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<FridgeResponse>.NotFound("Family not found for the current user.");

        var fridge = await _fridgeRepository.GetByIdAsync(id, cancellationToken);
        if (fridge is null)
            return Result<FridgeResponse>.NotFound("Fridge not found.");
        if (fridge.FamilyId != family.Id)
            return Result<FridgeResponse>.Unauthorized("Access denied.");

        return Result<FridgeResponse>.Success(Map(fridge));
    }

    public async Task<Result<FridgeResponse>> UpdateAsync(
        Guid id,
        UpdateFridgeRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<FridgeResponse>.NotFound("Family not found for the current user.");

        var fridge = await _fridgeRepository.GetByIdAsync(id, cancellationToken);
        if (fridge is null)
            return Result<FridgeResponse>.NotFound("Fridge not found.");
        if (fridge.FamilyId != family.Id)
            return Result<FridgeResponse>.Unauthorized("Access denied.");

        fridge.Name = request.Name;
        fridge.UpdatedAt = DateTime.UtcNow;

        var updated = await _fridgeRepository.UpdateAsync(fridge, cancellationToken);
        return Result<FridgeResponse>.Success(Map(updated));
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
            return Result<bool>.NotFound("Family not found for the current user.");

        var fridge = await _fridgeRepository.GetByIdAsync(id, cancellationToken);
        if (fridge is null)
            return Result<bool>.NotFound("Fridge not found.");
        if (fridge.FamilyId != family.Id)
            return Result<bool>.Unauthorized("Access denied.");

        await _fridgeRepository.SoftDeleteAsync(id, cancellationToken);
        return Result<bool>.Success(true);
    }

    private static FridgeResponse Map(Fridge f) =>
        new() { Id = f.Id, Name = f.Name, FamilyId = f.FamilyId };
}
