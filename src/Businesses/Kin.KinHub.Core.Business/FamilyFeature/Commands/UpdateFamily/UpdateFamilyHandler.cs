namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class UpdateFamilyHandler : IUpdateFamilyHandler
{
    private readonly IFamilyOwnershipService _familyOwnershipService;
    private readonly IFamilyRepository _familyRepository;

    public UpdateFamilyHandler(
        IFamilyOwnershipService familyOwnershipService,
        IFamilyRepository familyRepository)
    {
        _familyOwnershipService = familyOwnershipService;
        _familyRepository = familyRepository;
    }

    public async Task<Result<UpdateFamilyResponse>> HandleAsync(
        Guid familyId,
        UpdateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _familyOwnershipService.EnsureOwnershipAsync(familyId, userId, cancellationToken);
            if (!access.IsSuccess)
            {
                return access.ToResult<UpdateFamilyResponse>();
            }

            var family = access.Family!;
            family.Name = request.Name;
            family.UpdatedAt = DateTime.UtcNow;
            await _familyRepository.UpdateAsync(family.Id, family, cancellationToken);

            return Result<UpdateFamilyResponse>.Success(new UpdateFamilyResponse
            {
                FamilyId = family.Id,
                Name = family.Name,
            });
        }
        catch (EntityNotFoundException ex)
        {
            return Result<UpdateFamilyResponse>.NotFound(ex.Message);
        }
        catch (SharedDomainException ex)
        {
            return Result<UpdateFamilyResponse>.UnexpectedError(ex.Message);
        }
    }
}
