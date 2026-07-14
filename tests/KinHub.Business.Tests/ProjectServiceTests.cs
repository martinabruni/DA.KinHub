using Xunit;
namespace KinHub.Business.Tests;

public class ProjectServiceTests
{
    [Fact]
    public void Creates_project()
    {
        Assert.Equal("Kitchen", new ProjectService().Create("Kitchen").Name);
    }
}
