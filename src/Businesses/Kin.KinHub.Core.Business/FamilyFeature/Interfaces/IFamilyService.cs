using Kin.KinHub.Core.Business.Common;

namespace Kin.KinHub.Core.Business.FamilyFeature;

/// <summary>
/// Business service for family management operations.
/// </summary>
public interface IFamilyService
{
    /// <summary>
    /// Creates a new family for the authenticated user with an initial owner profile and optional additional members.
    /// </summary>
    Task<Result<CreateFamilyResponse>> CreateFamilyAsync(
        CreateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new member profile to an existing family owned by the given user.
    /// </summary>
    Task<Result<AddFamilyMemberResponse>> AddFamilyMemberAsync(
        Guid familyId,
        AddFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the family and its members for the given user.
    /// </summary>
    Task<Result<FamilyDetailResponse>> GetFamilyAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a family member by the given user (ownership validated).
    /// </summary>
    Task<Result<bool>> DeleteFamilyMemberAsync(
        Guid familyId,
        Guid memberId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the name of a family member (ownership and uniqueness validated).
    /// </summary>
    Task<Result<UpdateFamilyMemberResponse>> UpdateFamilyMemberAsync(
        Guid familyId,
        Guid memberId,
        UpdateFamilyMemberRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the name of a family (ownership validated).
    /// </summary>
    Task<Result<UpdateFamilyResponse>> UpdateFamilyAsync(
        Guid familyId,
        UpdateFamilyRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a family and all its members (ownership validated).
    /// </summary>
    Task<Result<bool>> DeleteFamilyAsync(
        Guid familyId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
