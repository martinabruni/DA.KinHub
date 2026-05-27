namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface IGetFamilyHandler
{
    Task<Result<FamilyDetailResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class GetFamilyHandler : IGetFamilyHandler
{
    private readonly IFamilyOwnershipService _familyOwnershipService;
    private readonly IFamilyMemberRepository _familyMemberRepository;

    public GetFamilyHandler(
        IFamilyOwnershipService familyOwnershipService,
        IFamilyMemberRepository familyMemberRepository)
    {
        _familyOwnershipService = familyOwnershipService;
        _familyMemberRepository = familyMemberRepository;
    }

    public async Task<Result<FamilyDetailResponse>> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var access = await _familyOwnershipService.GetCurrentFamilyAsync(userId, cancellationToken);
            if (!access.IsSuccess)
                return access.ToResult<FamilyDetailResponse>();

            var members = await _familyMemberRepository.GetByFamilyIdAsync(access.Family!.Id, cancellationToken);

            return Result<FamilyDetailResponse>.Success(new FamilyDetailResponse
            {
                Id = access.Family.Id,
                Name = access.Family.Name,
                Members = members.Select(member => new FamilyMemberDto
                {
                    Id = member.Id,
                    Name = member.Name,
                }).ToList(),
            });
        }
        catch (DomainException ex)
        {
            return Result<FamilyDetailResponse>.UnexpectedError(ex.Message);
        }
    }
}
