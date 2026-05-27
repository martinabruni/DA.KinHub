namespace Kin.KinHub.Core.Business.FamilyFeature;

public interface ICreateFamilyHandler
{
    Task<Result<CreateFamilyResponse>> HandleAsync(
        CreateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class CreateFamilyHandler : ICreateFamilyHandler
{
    private readonly IFamilyRepository _familyRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IKinHubServiceRepository _kinHubServiceRepository;
    private readonly IFamilyServiceRepository _familyServiceRepository;

    public CreateFamilyHandler(
        IFamilyRepository familyRepository,
        IFamilyMemberRepository familyMemberRepository,
        IKinHubServiceRepository kinHubServiceRepository,
        IFamilyServiceRepository familyServiceRepository)
    {
        _familyRepository = familyRepository;
        _familyMemberRepository = familyMemberRepository;
        _kinHubServiceRepository = kinHubServiceRepository;
        _familyServiceRepository = familyServiceRepository;
    }

    public async Task<Result<CreateFamilyResponse>> HandleAsync(
        CreateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
            if (existing is not null)
                return Result<CreateFamilyResponse>.Conflict("A family already exists for this user.");

            var now = DateTime.UtcNow;

            var family = new Family
            {
                Id = Guid.NewGuid(),
                Name = request.FamilyName,
                UserId = userId,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var createdFamily = await _familyRepository.CreateAsync(family);

            var ownerMember = new FamilyMember
            {
                Id = Guid.NewGuid(),
                Name = request.OwnerProfileName,
                FamilyId = createdFamily.Id,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var createdOwner = await _familyMemberRepository.CreateAsync(ownerMember);

            foreach (var memberName in request.AdditionalMembers)
            {
                await _familyMemberRepository.CreateAsync(new FamilyMember
                {
                    Id = Guid.NewGuid(),
                    Name = memberName,
                    FamilyId = createdFamily.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            var allServices = await _kinHubServiceRepository.GetAllAsync();
            foreach (var service in allServices)
            {
                await _familyServiceRepository.CreateAsync(new FamilyService
                {
                    Id = Guid.NewGuid(),
                    FamilyId = createdFamily.Id,
                    ServiceId = service.Id,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            return Result<CreateFamilyResponse>.Success(new CreateFamilyResponse
            {
                FamilyId = createdFamily.Id,
                OwnerMemberId = createdOwner.Id,
            });
        }
        catch (DuplicateEntityException ex)
        {
            return Result<CreateFamilyResponse>.Conflict(ex.Message);
        }
        catch (DomainException ex)
        {
            return Result<CreateFamilyResponse>.UnexpectedError(ex.Message);
        }
    }
}
