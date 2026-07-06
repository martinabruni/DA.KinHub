using Microsoft.Extensions.Logging;

namespace Kin.KinHub.Core.Business.FamilyFeature;

public sealed class FamilyAccessResult
{
    private FamilyAccessResult(bool isSuccess, Family? family, ResultStatus status, string? message)
    {
        IsSuccess = isSuccess;
        Family = family;
        Status = status;
        Message = message;
    }

    public bool IsSuccess { get; }

    public Family? Family { get; }

    public ResultStatus Status { get; }

    public string? Message { get; }

    public static FamilyAccessResult Success(Family family) =>
        new(true, family, ResultStatus.Success, null);

    public static FamilyAccessResult NotFound(string message) =>
        new(false, null, ResultStatus.NotFound, message);

    public static FamilyAccessResult Unauthorized(string message) =>
        new(false, null, ResultStatus.Unauthorized, message);

    public static FamilyAccessResult ServiceUnavailable(string message) =>
        new(false, null, ResultStatus.ServiceUnavailable, message);

    public Result<T> ToResult<T>() =>
        Status switch
        {
            ResultStatus.NotFound => Result<T>.NotFound(Message!),
            ResultStatus.Unauthorized => Result<T>.Unauthorized(Message!),
            ResultStatus.ServiceUnavailable => Result<T>.ServiceUnavailable(Message!),
            _ => Result<T>.UnexpectedError(Message ?? "Unexpected family access state."),
        };

    /// <summary>
    /// Verifies the resolved family owns <paramref name="requestedFamilyId"/>. Failed results are
    /// returned unchanged; a mismatch yields <see cref="Unauthorized"/>. The single source of truth
    /// for the ownership rule shared by local and remote ownership services.
    /// </summary>
    /// <param name="requestedFamilyId">The family id the caller is trying to act on.</param>
    /// <param name="onDenied">Optional callback invoked with the owned family id when access is denied.</param>
    public FamilyAccessResult EnsureOwnership(Guid requestedFamilyId, Action<Guid>? onDenied = null)
    {
        if (!IsSuccess)
        {
            return this;
        }

        if (Family!.Id != requestedFamilyId)
        {
            onDenied?.Invoke(Family.Id);
            return Unauthorized("You do not own this family.");
        }

        return this;
    }
}

public interface IFamilyOwnershipService
{
    Task<FamilyAccessResult> GetCurrentFamilyAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<FamilyAccessResult> EnsureOwnershipAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class FamilyOwnershipService : IFamilyOwnershipService
{
    private readonly IFamilyRepository _familyRepository;
    private readonly ILogger<FamilyOwnershipService> _logger;

    public FamilyOwnershipService(
        IFamilyRepository familyRepository,
        ILogger<FamilyOwnershipService> logger)
    {
        _familyRepository = familyRepository;
        _logger = logger;
    }

    public async Task<FamilyAccessResult> GetCurrentFamilyAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var family = await _familyRepository.FindByUserIdAsync(userId, cancellationToken);
        if (family is null)
        {
            _logger.LogWarning("Family lookup failed for user {UserId}.", userId);
            return FamilyAccessResult.NotFound("Family not found for this user.");
        }

        return FamilyAccessResult.Success(family);
    }

    public async Task<FamilyAccessResult> EnsureOwnershipAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var currentFamily = await GetCurrentFamilyAsync(userId, cancellationToken);
        return currentFamily.EnsureOwnership(
            familyId,
            ownedFamilyId => _logger.LogWarning(
                "Family ownership denied for user {UserId}. Requested family {RequestedFamilyId}, owned family {OwnedFamilyId}.",
                userId,
                familyId,
                ownedFamilyId));
    }
}
