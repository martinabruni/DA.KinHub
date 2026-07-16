using AdvancedFrontier.Business.Common;
using AdvancedFrontier.Domain.Common;
using AdvancedFrontier.Domain.Projects;

namespace AdvancedFrontier.Business.Projects;

public sealed class ProjectService(IFamilyProjectRepository repository, TimeProvider timeProvider) : IProjectService
{
    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        ProjectName name;
        try
        {
            name = new ProjectName(request.Name);
        }
        catch (DomainException exception)
        {
            throw new BusinessValidationException("projects.invalidName", exception.Message);
        }

        if (await repository.ExistsByNameAsync(name, cancellationToken))
        {
            throw new BusinessValidationException("projects.duplicateName", "A project with the same name already exists.");
        }

        var project = FamilyProject.Create(name, timeProvider.GetUtcNow());
        await repository.AddAsync(project, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    public async Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken) =>
        (await repository.ListAsync(cancellationToken)).Select(Map).ToArray();

    private static ProjectDto Map(FamilyProject project) =>
        new(project.Id, project.Name.Value, project.Stage.ToString(), project.CreatedAt);
}
