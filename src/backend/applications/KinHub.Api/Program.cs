using KinHub.Business;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IProjectService, ProjectService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true);
    });
});
var app = builder.Build();
app.UseExceptionHandler();
app.UseCors();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapGet("/api/version", (IHostEnvironment env) => Results.Ok(new
{
    appName = "KinHub",
    semanticVersion = "0.1.0",
    commitSha = Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "local",
    buildDate = Environment.GetEnvironmentVariable("BUILD_DATE") ?? "unknown",
    environment = env.EnvironmentName,
    apiVersion = "1"
}));
app.MapGet("/api/status", () => Results.Ok(new
{
    status = "ok",
    appName = "KinHub"
}));
app.MapPost("/api/projects", (ProjectRequest request, IProjectService service) =>
    Results.Created($"/api/projects/{Guid.NewGuid()}", service.Create(request.Name)));
app.Run();

public sealed record ProjectRequest(string Name);

public partial class Program
{
}
