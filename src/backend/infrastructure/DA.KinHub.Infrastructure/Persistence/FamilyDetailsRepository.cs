using System.Data.Common;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyDetailsRepository(KinHubDbContext dbContext) : IFamilyDetailsRepository
{
    public async Task<FamilyDetailsEntry?> GetFamilyDetailsAsync(Guid familyId, CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.Families
                .AsNoTracking()
                .Where(family => family.Id == familyId && family.InactiveAt == null)
                .Select(family => new FamilyDetailsEntry(family.Name.Value))
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (Exception exception) when (IsRepositoryUnavailable(exception))
        {
            throw new RepositoryUnavailableException("The family details could not be loaded.", exception);
        }
    }

    private static bool IsRepositoryUnavailable(Exception exception)
        => exception is TimeoutException
        or DbException
        or DbUpdateException { InnerException: DbException };
}
