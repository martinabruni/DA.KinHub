using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class KinHubFamilyService : IFamilyService
{
    private readonly IFamilyRepository _familyRepository;
    private readonly IFamilyMemberRepository _familyMemberRepository;
    private readonly IKinHubServiceRepository _kinHubServiceRepository;
    private readonly IFamilyServiceRepository _familyServiceRepository;

    public KinHubFamilyService(
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

    /// <inheritdoc/>
    public async Task<Result<CreateFamilyResponse>> CreateFamilyAsync(
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
                var additionalMember = new FamilyMember
                {
                    Id = Guid.NewGuid(),
                    Name = memberName,
                    FamilyId = createdFamily.Id,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                await _familyMemberRepository.CreateAsync(additionalMember);
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
        catch (Exception ex)
        {
            return Result<CreateFamilyResponse>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<AddFamilyMemberResponse>> AddFamilyMemberAsync(
        Guid familyId,
        AddFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
            if (family is null)
                return Result<AddFamilyMemberResponse>.NotFound("Family not found for this user.");

            if (family.Id != familyId)
                return Result<AddFamilyMemberResponse>.Unauthorized("You do not own this family.");

            var now = DateTime.UtcNow;

            var newMember = new FamilyMember
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                FamilyId = familyId,
                CreatedAt = now,
                UpdatedAt = now,
            };

            var createdMember = await _familyMemberRepository.CreateAsync(newMember);

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
        catch (Exception ex)
        {
            return Result<AddFamilyMemberResponse>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<FamilyDetailResponse>> GetFamilyAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
            if (family is null)
                return Result<FamilyDetailResponse>.NotFound("Family not found for this user.");

            var members = await _familyMemberRepository.GetByFamilyIdAsync(family.Id, cancellationToken);

            var memberDtos = members.Select(m => new FamilyMemberDto
            {
                Id = m.Id,
                Name = m.Name,
            }).ToList();

            return Result<FamilyDetailResponse>.Success(new FamilyDetailResponse
            {
                FamilyId = family.Id,
                Name = family.Name,
                Members = memberDtos,
            });
        }
        catch (Exception ex)
        {
            return Result<FamilyDetailResponse>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> DeleteFamilyMemberAsync(
        Guid familyId,
        Guid memberId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
            if (family is null)
                return Result<bool>.NotFound("Family not found for this user.");

            if (family.Id != familyId)
                return Result<bool>.Unauthorized("You do not own this family.");

            var member = await _familyMemberRepository.GetAsync(memberId);

            if (member.FamilyId != familyId)
                return Result<bool>.NotFound("Member not found in this family.");

            var allMembers = await _familyMemberRepository.GetByFamilyIdAsync(familyId, cancellationToken);
            if (allMembers.Count <= 1)
                return Result<bool>.Conflict("Cannot remove the only member of a family.");

            member.IsDeleted = true;
            member.UpdatedAt = DateTime.UtcNow;
            await _familyMemberRepository.UpdateAsync(member.Id, member);

            return Result<bool>.Success(true);
        }
        catch (EntityNotFoundException ex)
        {
            return Result<bool>.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<bool>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<UpdateFamilyMemberResponse>> UpdateFamilyMemberAsync(
        Guid familyId,
        Guid memberId,
        UpdateFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
            if (family is null)
                return Result<UpdateFamilyMemberResponse>.NotFound("Family not found for this user.");

            if (family.Id != familyId)
                return Result<UpdateFamilyMemberResponse>.Unauthorized("You do not own this family.");

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
        catch (Exception ex)
        {
            return Result<UpdateFamilyMemberResponse>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<UpdateFamilyResponse>> UpdateFamilyAsync(
        Guid familyId,
        UpdateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
            if (family is null)
                return Result<UpdateFamilyResponse>.NotFound("Family not found for this user.");

            if (family.Id != familyId)
                return Result<UpdateFamilyResponse>.Unauthorized("You do not own this family.");

            family.Name = request.Name;
            family.UpdatedAt = DateTime.UtcNow;
            await _familyRepository.UpdateAsync(family.Id, family);

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
        catch (Exception ex)
        {
            return Result<UpdateFamilyResponse>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> DeleteFamilyAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
            if (family is null)
                return Result<bool>.NotFound("Family not found for this user.");

            if (family.Id != familyId)
                return Result<bool>.Unauthorized("You do not own this family.");

            var members = await _familyMemberRepository.GetByFamilyIdAsync(familyId, cancellationToken);
            var now = DateTime.UtcNow;

            foreach (var member in members)
            {
                member.IsDeleted = true;
                member.UpdatedAt = now;
                await _familyMemberRepository.UpdateAsync(member.Id, member);
            }

            family.IsDeleted = true;
            family.UpdatedAt = now;
            await _familyRepository.UpdateAsync(family.Id, family);

            return Result<bool>.Success(true);
        }
        catch (EntityNotFoundException ex)
        {
            return Result<bool>.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Result<bool>.UnexpectedError(ex.Message);
        }
    }
}
