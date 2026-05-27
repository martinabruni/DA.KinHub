namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IUpdateFamilyMemberHandler
{
    Task<Result<UpdateFamilyMemberResponse>> HandleAsync(
        Guid familyId,
        Guid memberId,
        UpdateFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateFamilyMemberHandler : IUpdateFamilyMemberHandler
{
    private readonly IFamilyOwnershipService _familyOwnershipService;
    private readonly IFamilyMemberRepository _familyMemberRepository;

    public UpdateFamilyMemberHandler(
        IFamilyOwnershipService familyOwnershipService,
        IFamilyMemberRepository familyMemberRepository)
    {
        _familyOwnershipService = familyOwnershipService;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<Result<UpdateFamilyMemberResponse>> HandleAsync(
        Guid familyId,
        Guid memberId,
        UpdateFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _familyOwnershipService.EnsureOwnershipAsync(familyId, userId, cancellationToken);
            if (!access.IsSuccess)
                return access.ToResult<UpdateFamilyMemberResponse>();

            var member = await _familyMemberRepository.GetAsync(memberId);
            if (member.FamilyId != familyId || member.IsDeleted)
                return Result<UpdateFamilyMemberResponse>.NotFound("Member not found in this family.");

            var existing = await _familyMemberRepository.FindByNameAsync(familyId, request.Name, cancellationToken);
            if (existing is not null && existing.Id != memberId)
                return Result<UpdateFamilyMemberResponse>.Conflict("A member with this name already exists in the family.");

            member.Name = request.Name;
            member.UpdatedAt = DateTime.UtcNow;
            await _familyMemberRepository.UpdateAsync(member.Id, member);

            return Result<UpdateFamilyMemberResponse>.Success(new UpdateFamilyMemberResponse
            {
                Id = member.Id,
                Name = member.Name,
            });
        }
        catch (EntityNotFoundException ex)
        {
            return Result<UpdateFamilyMemberResponse>.NotFound(ex.Message);
        }
        catch (DomainException ex)
        {
            return Result<UpdateFamilyMemberResponse>.UnexpectedError(ex.Message);
        }
    }
}
