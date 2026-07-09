using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;
namespace Kin.KinHub.Core.Domain.FamilyFeature;

/// <summary>
/// Repository contract for <see cref="FamilyMember"/> aggregates.
/// </summary>
public interface IFamilyMemberRepository : IRepository<FamilyMember, Guid>
{
    /// <summary>
    /// Returns all members belonging to the given family.
    /// </summary>
    Task<IReadOnlyList<FamilyMember>> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the member with the given name within the family, or null if not found.
    /// </summary>
    Task<FamilyMember?> FindByNameAsync(Guid familyId, string name, CancellationToken cancellationToken = default);
}
