namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class DeleteFamilyMemberHandler : IDeleteFamilyMemberHandler
{
    private readonly IFamilyOwnershipService _familyOwnershipService;
    private readonly IFamilyMemberRepository _familyMemberRepository;

    public DeleteFamilyMemberHandler(
        IFamilyOwnershipService familyOwnershipService,
        IFamilyMemberRepository familyMemberRepository)
    {
        _familyOwnershipService = familyOwnershipService;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<Result<bool>> HandleAsync(
        Guid familyId,
        Guid memberId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _familyOwnershipService.EnsureOwnershipAsync(familyId, userId, cancellationToken);
            if (!access.IsSuccess)
            {
                return access.ToResult<bool>();
            }

            var member = await _familyMemberRepository.GetAsync(memberId);
            if (member.FamilyId != familyId)
            {
                return Result<bool>.NotFound("Member not found in this family.");
            }

            var allMembers = await _familyMemberRepository.GetByFamilyIdAsync(familyId, cancellationToken);
            if (allMembers.Count <= 1)
            {
                return Result<bool>.Conflict("Cannot remove the only member of a family.");
            }

            member.IsDeleted = true;
            member.UpdatedAt = DateTime.UtcNow;
            await _familyMemberRepository.UpdateAsync(member.Id, member);

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
