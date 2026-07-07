namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class AddFamilyMemberHandler : IAddFamilyMemberHandler
{
    private readonly IFamilyOwnershipService _familyOwnershipService;
    private readonly IFamilyMemberRepository _familyMemberRepository;

    public AddFamilyMemberHandler(
        IFamilyOwnershipService familyOwnershipService,
        IFamilyMemberRepository familyMemberRepository)
    {
        _familyOwnershipService = familyOwnershipService;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<Result<AddFamilyMemberResponse>> HandleAsync(
        Guid familyId,
        AddFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _familyOwnershipService.EnsureOwnershipAsync(familyId, userId, cancellationToken);
            if (!access.IsSuccess)
            {
                return access.ToResult<AddFamilyMemberResponse>();
            }

            var now = DateTime.UtcNow;
            var createdMember = await _familyMemberRepository.CreateAsync(new FamilyMember
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                FamilyId = familyId,
                CreatedAt = now,
                UpdatedAt = now,
            });

            return Result<AddFamilyMemberResponse>.Success(new AddFamilyMemberResponse
            {
                MemberId = createdMember.Id,
            });
        }
        catch (DuplicateEntityException ex)
        {
            return Result<AddFamilyMemberResponse>.Conflict(ex.Message);
        }
        catch (EntityNotFoundException ex)
        {
            return Result<AddFamilyMemberResponse>.NotFound(ex.Message);
        }
        catch (SharedDomainException ex)
        {
            return Result<AddFamilyMemberResponse>.UnexpectedError(ex.Message);
        }
    }
}
