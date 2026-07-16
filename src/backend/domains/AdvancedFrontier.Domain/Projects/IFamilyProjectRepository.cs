namespace AdvancedFrontier.Domain.Projects;

public interface IFamilyProjectRepository
{
    Task<bool> ExistsByNameAsync(ProjectName name, CancellationToken cancellationToken);

    Task AddAsync(FamilyProject project, CancellationToken cancellationToken);

    Task<IReadOnlyList<FamilyProject>> ListAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
