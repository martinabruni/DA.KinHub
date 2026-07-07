namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IDeleteFamilyHandler
{
    Task<Result<bool>> HandleAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class DeleteFamilyHandler : IDeleteFamilyHandler
{
    private readonly IFamilyOwnershipService _familyOwnershipService;
    private readonly IFamilyRepository _familyRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;

    public DeleteFamilyHandler(
        IFamilyOwnershipService familyOwnershipService,
        IFamilyRepository familyRepository,
        IFamilyMemberRepository familyMemberRepository)
    {
        _familyOwnershipService = familyOwnershipService;
        _familyRepository = familyRepository;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _familyOwnershipService.EnsureOwnershipAsync(familyId, userId, cancellationToken);
            if (!access.IsSuccess)
                return access.ToResult<bool>();

            var members = await _familyMemberRepository.GetByFamilyIdAsync(familyId, cancellationToken);
            var now = DateTime.UtcNow;

            foreach (var member in members)
            {
                member.IsDeleted = true;
                member.UpdatedAt = now;
                await _familyMemberRepository.UpdateAsync(member.Id, member);
            }

            var family = access.Family!;
            family.IsDeleted = true;
            family.UpdatedAt = now;
            await _familyRepository.UpdateAsync(family.Id, family);

            return Result<bool>.Success(true);
        }
        catch (EntityNotFoundException ex)
        {
            return Result<bool>.NotFound(ex.Message);
        }
        catch (SharedDomainException ex)
        {
            return Result<bool>.UnexpectedError(ex.Message);
        }
    }
}
