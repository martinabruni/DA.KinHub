using Kin.KinHub.Identity.PostgreSql;
using Kin.KinHub.Shared.Kernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");
builder.Services.AddKinHubIdentityApi(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Configuration.GetValue("RunMigrationsOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var identityContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await identityContext.ApplyPendingMigrationsAsync(logger);
}

app.UseKinHubIdentityApi();

app.Run();
