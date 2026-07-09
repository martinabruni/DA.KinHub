using Kin.KinHub.Core.PostgreSql;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.PostgreSql;
using Kin.KinHub.KinRecipe.PostgreSql;
using Kin.KinHub.Shared.Kernel.Extensions;
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

    if (builder.Configuration.GetValue("RunMigrationsOnStartup", true))
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await scope.ServiceProvider.GetRequiredService<CoreDbContext>().ApplyPendingMigrationsAsync(logger);
        await scope.ServiceProvider.GetRequiredService<KinListDbContext>().ApplyPendingMigrationsAsync(logger);
        await scope.ServiceProvider.GetRequiredService<KinRecipeDbContext>().ApplyPendingMigrationsAsync(logger);
    }
}

host.Run();

public partial class Program;
