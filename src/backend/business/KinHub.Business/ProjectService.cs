using KinHub.Domain;

namespace KinHub.Business;

public interface IProjectService
{
    Project Create(string name);
}

public sealed class ProjectService : IProjectService
{
    public Project Create(string name) => new(name);
}
