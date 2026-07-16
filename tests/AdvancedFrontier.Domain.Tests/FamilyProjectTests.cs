using AdvancedFrontier.Domain.Common;
using AdvancedFrontier.Domain.Projects;

namespace AdvancedFrontier.Domain.Tests;

public sealed class FamilyProjectTests
{
    [Fact]
    public void ProjectCanOnlyAdvanceForward()
    {
        var project = FamilyProject.Create(new ProjectName("Summer holiday"), DateTimeOffset.UtcNow);

        project.AdvanceTo(ProjectStage.Planning);

        Assert.Equal(ProjectStage.Planning, project.Stage);
        Assert.Throws<DomainException>(() => project.AdvanceTo(ProjectStage.Discovery));
    }
}
