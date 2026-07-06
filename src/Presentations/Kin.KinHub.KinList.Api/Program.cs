using Kin.KinHub.KinList.Business.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");
builder.Services.AddKinHubKinListApi(builder.Configuration, builder.Environment);

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    KinListTransactionExecutorGuard.EnsureConfigured(
        scope.ServiceProvider.GetRequiredService<IKinListTransactionExecutor>(),
        app.Environment.IsDevelopment());
}

app.UseKinHubKinListApi();

app.Run();

/// <summary>
/// Explicit entry-point marker so integration tests can bootstrap the KinList API host
/// through <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public partial class Program;
