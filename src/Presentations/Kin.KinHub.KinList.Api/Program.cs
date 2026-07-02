var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");
builder.Services.AddKinHubKinListApi(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseKinHubKinListApi();

app.Run();

/// <summary>
/// Explicit entry-point marker so integration tests can bootstrap the KinList API host
/// through <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public partial class Program;
