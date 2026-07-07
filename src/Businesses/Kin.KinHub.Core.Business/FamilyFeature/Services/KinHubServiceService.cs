using Kin.KinHub.Core.Business.Common;
using Mapster;

namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class KinHubServiceService : IKinHubServiceService
{
    private readonly IKinHubServiceRepository _kinHubServiceRepository;
    private readonly IFamilyServiceRepository _familyServiceRepository;
    private readonly IFamilyOwnershipService _familyOwnershipService;

    public KinHubServiceService(
        IKinHubServiceRepository kinHubServiceRepository,
        IFamilyServiceRepository familyServiceRepository,
        IFamilyOwnershipService familyOwnershipService)
    {
        _kinHubServiceRepository = kinHubServiceRepository;
        _familyServiceRepository = familyServiceRepository;
        _familyOwnershipService = familyOwnershipService;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<KinHubServiceDto>>> GetAllServicesAsync(
        CancellationToken cancellationToken = default)
    {
        var services = await _kinHubServiceRepository.GetAllAsync(cancellationToken);

        return Result<IReadOnlyList<KinHubServiceDto>>.Success(services.Adapt<List<KinHubServiceDto>>());
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<FamilyServiceDto>>> GetFamilyServicesAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _familyOwnershipService.EnsureOwnershipAsync(familyId, userId, cancellationToken);
        if (!access.IsSuccess)
            return access.ToResult<IReadOnlyList<FamilyServiceDto>>();

        var allServices = await _kinHubServiceRepository.GetAllAsync(cancellationToken);
        var familyServices = await _familyServiceRepository.GetByFamilyIdAsync(familyId, cancellationToken);

        var dtos = allServices
            .Select(s =>
            {
                var fs = familyServices.FirstOrDefault(f => f.ServiceId == s.Id);
                return new FamilyServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsEnabled = fs?.IsActive ?? false,
                };
            })
            .ToList();

        return Result<IReadOnlyList<FamilyServiceDto>>.Success(dtos);
    }

    /// <inheritdoc/>
    public async Task<Result<FamilyServiceDto>> ToggleFamilyServiceAsync(
        Guid familyId,
        ToggleFamilyServiceRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var access = await _familyOwnershipService.EnsureOwnershipAsync(familyId, userId, cancellationToken);
        if (!access.IsSuccess)
            return access.ToResult<FamilyServiceDto>();

        if (request.ServiceId == (int)KinHubServiceType.KinConsole && !request.IsActive)
            return Result<FamilyServiceDto>.ValidationError("KinConsole non puÃ² essere disattivato.");

        var now = DateTime.UtcNow;

        var existing = await _familyServiceRepository.FindByFamilyAndServiceAsync(
            familyId,
            request.ServiceId,
            cancellationToken);

        FamilyService familyService;

        if (existing is null)
        {
            familyService = await _familyServiceRepository.CreateAsync(new FamilyService
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                ServiceId = request.ServiceId,
                IsActive = request.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
            }, cancellationToken);
        }
        else
        {
            existing.IsActive = request.IsActive;
            existing.UpdatedAt = now;
            familyService = await _familyServiceRepository.UpdateAsync(existing.Id, existing, cancellationToken);
        }

        var service = await _kinHubServiceRepository.FindByServiceTypeAsync(
            (KinHubServiceType)request.ServiceId,
            cancellationToken);

        return Result<FamilyServiceDto>.Success(new FamilyServiceDto
        {
            Id = familyService.ServiceId,
            Name = service?.Name ?? string.Empty,
            IsEnabled = familyService.IsActive,
        });
    }
}
