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
    private readonly ICoreTransactionExecutor _transactionExecutor;

    public CreateFamilyHandler(
        IFamilyRepository familyRepository,
        IFamilyMemberRepository familyMemberRepository,
        IKinHubServiceRepository kinHubServiceRepository,
        IFamilyServiceRepository familyServiceRepository,
        ICoreTransactionExecutor transactionExecutor)
    {
        _familyRepository = familyRepository;
        _familyMemberRepository = familyMemberRepository;
        _kinHubServiceRepository = kinHubServiceRepository;
        _familyServiceRepository = familyServiceRepository;
        _transactionExecutor = transactionExecutor;
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

            return await _transactionExecutor.ExecuteAsync(async ct =>
            {
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

                var members = new List<FamilyMember>(request.AdditionalMembers.Count + 1)
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = request.OwnerProfileName,
                        FamilyId = createdFamily.Id,
                        CreatedAt = now,
                        UpdatedAt = now,
                    },
                };

                members.AddRange(request.AdditionalMembers.Select(memberName => new FamilyMember
                {
                    Id = Guid.NewGuid(),
                    Name = memberName,
                    FamilyId = createdFamily.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                }));

                var createdMembers = await _familyMemberRepository.CreateRangeAsync(members);
                var createdOwner = createdMembers[0];

                var allServices = await _kinHubServiceRepository.GetAllAsync();
                var familyServices = allServices.Select(service => new FamilyService
                {
                    Id = Guid.NewGuid(),
                    FamilyId = createdFamily.Id,
                    ServiceId = service.Id,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToArray();
                await _familyServiceRepository.CreateRangeAsync(familyServices);

                return Result<CreateFamilyResponse>.Success(new CreateFamilyResponse
                {
                    FamilyId = createdFamily.Id,
                    OwnerMemberId = createdOwner.Id,
                });
            }, cancellationToken);
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
