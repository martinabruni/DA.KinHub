using AdvancedFrontier.Business.Common;
using AdvancedFrontier.Business.Projects;
using AdvancedFrontier.Domain.Projects;

namespace AdvancedFrontier.Business.Tests;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task DuplicateProjectNameIsRejected()
    {
        var repository = new InMemoryRepository();
        var service = new ProjectService(repository, TimeProvider.System);
        await service.CreateAsync(new CreateProjectRequest("Weekly menu"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.CreateAsync(new CreateProjectRequest("Weekly menu"), CancellationToken.None));

        Assert.Equal("projects.duplicateName", exception.Code);
    }

    [Fact]
    public async Task ShortNameIsRejected()
    {
        var service = new ProjectService(new InMemoryRepository(), TimeProvider.System);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.CreateAsync(new CreateProjectRequest("x"), CancellationToken.None));

        Assert.Equal("projects.invalidName", exception.Code);
    }

    private sealed class InMemoryRepository : IFamilyProjectRepository
    {
        private readonly List<FamilyProject> projects = [];

        public Task<bool> ExistsByNameAsync(ProjectName name, CancellationToken cancellationToken) =>
            Task.FromResult(projects.Any(project => project.Name == name));

        public Task AddAsync(FamilyProject project, CancellationToken cancellationToken)
        {
            projects.Add(project);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FamilyProject>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FamilyProject>>(projects);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
