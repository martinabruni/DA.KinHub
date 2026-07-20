using DA.KinHub.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyProjectRepository(KinHubDbContext dbContext) : IFamilyProjectRepository
{
    public Task<bool> ExistsByNameAsync(ProjectName name, CancellationToken cancellationToken) =>
        dbContext.Projects.AnyAsync(project => project.Name == name, cancellationToken);

    public async Task AddAsync(FamilyProject project, CancellationToken cancellationToken) =>
        await dbContext.Projects.AddAsync(project, cancellationToken);

    public async Task<IReadOnlyList<FamilyProject>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Projects.AsNoTracking().OrderBy(project => project.CreatedAt).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
