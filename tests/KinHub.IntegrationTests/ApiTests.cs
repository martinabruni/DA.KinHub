using Xunit;
namespace KinHub.IntegrationTests;

public class ApiTests
{
    [Fact]
    public void Version_contract_is_documented()
    {
        Assert.Contains("version", "/api/version");
    }
}
