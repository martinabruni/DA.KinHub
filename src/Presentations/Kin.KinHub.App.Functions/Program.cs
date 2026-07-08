using Kin.KinHub.KinList.Business.Common;
using Microsoft.Azure.Functions.Worker.Builder;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");
builder.ConfigureFunctionsWebApplication();
builder.Services.AddKinHubAppFunctions(builder.Configuration, builder.Environment);

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    KinListTransactionExecutorGuard.EnsureConfigured(
        scope.ServiceProvider.GetRequiredService<IKinListTransactionExecutor>(),
        builder.Environment.IsDevelopment());
}

host.Run();

public partial class Program;
