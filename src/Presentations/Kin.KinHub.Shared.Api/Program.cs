using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped, includeInternalTypes: true)
    .AddScoped(typeof(IRequestValidator<>), typeof(FluentRequestValidator<>))
    .AddKinHubCorePostgreSqlInfrastructure(o => o.ConnectionString = builder.Configuration.GetConnectionString("KinHub")!)
    .AddKinHubIdentityPostgreSqlInfrastructure(o => o.ConnectionString = builder.Configuration.GetConnectionString("KinHub")!)
    .AddKinHubIdentityJwtInfrastructure(o =>
    {
        o.Secret = builder.Configuration["Jwt:Secret"]
            ?? "CHANGE-ME-use-a-long-random-secret-at-least-32-chars!";
        o.AccessTokenExpiryMinutes = int.Parse(builder.Configuration["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        o.Issuer = builder.Configuration["Jwt:Issuer"]
            ?? "kinhub";
    })
    .AddKinHubCoreBusiness()
    .AddKinHubIdentityBusiness()
    .AddKinHubCoreOpenAiInfrastructure(o =>
    {
        o.Endpoint = builder.Configuration["OpenAi:Endpoint"] ?? string.Empty;
        o.ApiKey = builder.Configuration["OpenAi:ApiKey"] ?? string.Empty;
        o.EmbeddingDeploymentName = builder.Configuration["OpenAi:EmbeddingDeploymentName"] ?? "text-embedding-3-small";
        o.ModelDeploymentName = builder.Configuration["OpenAi:ModelDeploymentName"] ?? "gpt-4o";
    });

builder.Services.AddOpenTelemetry().UseAzureMonitor();
builder.Services
    .AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("KinHub")!,
        name: "kinhub-dev-psqldb",
        timeout: TimeSpan.FromSeconds(10));

builder.Services.AddScoped<JwtAuthenticationMiddleware>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
