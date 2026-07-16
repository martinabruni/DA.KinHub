namespace AdvancedFrontier.Business.Projects;

public sealed record CreateProjectRequest(string Name);

public sealed record ProjectDto(Guid Id, string Name, string Stage, DateTimeOffset CreatedAt);

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken);
}
