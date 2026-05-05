using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class KinHubServiceService : IKinHubServiceService
{
    private readonly IKinHubServiceRepository _kinHubServiceRepository;
    private readonly IFamilyServiceRepository _familyServiceRepository;

    public KinHubServiceService(
        IKinHubServiceRepository kinHubServiceRepository,
        IFamilyServiceRepository familyServiceRepository)
    {
        _kinHubServiceRepository = kinHubServiceRepository;
        _familyServiceRepository = familyServiceRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<KinHubServiceDto>>> GetAllServicesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var services = await _kinHubServiceRepository.GetAllAsync();

            var dtos = services
                .Select(s => new KinHubServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    BaseUrl = s.BaseUrl,
                    IsActive = s.IsActive,
                    IsAdminOnly = s.IsAdminOnly,
                })
                .ToList();

            return Result<IReadOnlyList<KinHubServiceDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<KinHubServiceDto>>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<FamilyServiceDto>>> GetFamilyServicesAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allServices = await _kinHubServiceRepository.GetAllAsync();
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
        catch (Exception ex)
        {
            return Result<IReadOnlyList<FamilyServiceDto>>.UnexpectedError(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<FamilyServiceDto>> ToggleFamilyServiceAsync(
        Guid familyId,
        ToggleFamilyServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
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
                });
            }
            else
            {
                existing.IsActive = request.IsActive;
                existing.UpdatedAt = now;
                familyService = await _familyServiceRepository.UpdateAsync(existing.Id, existing);
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
        catch (Exception ex)
        {
            return Result<FamilyServiceDto>.UnexpectedError(ex.Message);
        }
    }
}
