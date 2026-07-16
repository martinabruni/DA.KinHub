using AdvancedFrontier.Domain.Common;

namespace AdvancedFrontier.Domain.Projects;

public sealed class FamilyProject
{
    private FamilyProject()
    {
    }

    private FamilyProject(Guid id, ProjectName name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
        Stage = ProjectStage.Discovery;
    }

    public Guid Id { get; private set; }

    public ProjectName Name { get; private set; }

    public ProjectStage Stage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static FamilyProject Create(ProjectName name, DateTimeOffset createdAt) => new(Guid.NewGuid(), name, createdAt);

    public void AdvanceTo(ProjectStage nextStage)
    {
        if (nextStage <= Stage)
        {
            throw new DomainException("A project can only advance to a later lifecycle stage.");
        }

        Stage = nextStage;
    }
}
