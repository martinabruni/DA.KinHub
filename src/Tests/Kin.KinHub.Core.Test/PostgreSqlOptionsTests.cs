using CorePostgreSqlOptions = Kin.KinHub.Core.PostgreSql.Common.PostgreSqlOptions;
using IdentityPostgreSqlOptions = Kin.KinHub.Identity.PostgreSql.Common.PostgreSqlOptions;

namespace Kin.KinHub.Core.Test;

public sealed class PostgreSqlOptionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_EmptyConnectionString_Throws(bool useIdentityOptions)
    {
        var validate = CreateValidateAction(useIdentityOptions, string.Empty);

        var exception = Assert.Throws<InvalidOperationException>(validate);

        Assert.Equal("ConnectionString must be configured.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_ConfiguredConnectionString_DoesNotThrow(bool useIdentityOptions)
    {
        var validate = CreateValidateAction(
            useIdentityOptions,
            "Host=localhost;Port=5432;Database=kinhub_tests;Username=kinhub;Password=kinhub");

        var exception = Record.Exception(validate);

        Assert.Null(exception);
    }

    private static Action CreateValidateAction(bool useIdentityOptions, string connectionString) =>
        useIdentityOptions
            ? () => new IdentityPostgreSqlOptions { ConnectionString = connectionString }.Validate()
            : () => new CorePostgreSqlOptions { ConnectionString = connectionString }.Validate();
}
