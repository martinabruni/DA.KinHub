using Kin.KinHub.Shared.Kernel.Options;

namespace Kin.KinHub.Core.Test;

public sealed class PostgreSqlOptionsTests
{
    [Fact]
    public void Validate_EmptyConnectionString_Throws()
    {
        var options = new PostgreSqlOptions { ConnectionString = string.Empty };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal("ConnectionString must be configured.", exception.Message);
    }

    [Fact]
    public void Validate_ConfiguredConnectionString_DoesNotThrow()
    {
        var options = new PostgreSqlOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Database=kinhub_tests;Username=kinhub;Password=kinhub",
        };

        var exception = Record.Exception(options.Validate);

        Assert.Null(exception);
    }
}
