using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");

var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new();
var openAiSettings = builder.Configuration.GetSection(OpenAiSettings.SectionName).Get<OpenAiSettings>() ?? new();
var mcpOptions = builder.Configuration.GetSection(McpTransportOptions.SectionName).Get<McpTransportOptions>() ?? new();

var effectiveJwtSecret = string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32
    ? "CHANGE-ME-use-a-long-random-secret-at-least-32-chars!"
    : jwtSettings.Secret;
var effectiveJwtIssuer = string.IsNullOrWhiteSpace(jwtSettings.Issuer)
    ? "kinhub"
    : jwtSettings.Issuer;

builder.Services.AddSingleton(corsOptions);
builder.Services.AddSingleton(mcpOptions);

builder.Services
    .AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped, includeInternalTypes: true)
    .AddScoped(typeof(IRequestValidator<>), typeof(FluentRequestValidator<>))
    .AddKinHubCorePostgreSqlInfrastructure(o => o.ConnectionString = builder.Configuration.GetConnectionString("KinHub")!)
    .AddKinHubIdentityPostgreSqlInfrastructure(o => o.ConnectionString = builder.Configuration.GetConnectionString("KinHub")!)
    .AddKinHubIdentityJwtInfrastructure(o =>
    {
        o.Secret = effectiveJwtSecret;
        o.AccessTokenExpiryMinutes = jwtSettings.AccessTokenExpiryMinutes;
        o.RefreshTokenExpiryDays = jwtSettings.RefreshTokenExpiryDays;
        o.Issuer = effectiveJwtIssuer;
    })
    .AddKinHubCoreBusiness()
    .AddKinHubIdentityBusiness()
    .AddKinHubCoreOpenAiInfrastructure(o =>
    {
        o.Endpoint = openAiSettings.Endpoint;
        o.ApiKey = openAiSettings.ApiKey;
        o.EmbeddingDeploymentName = openAiSettings.EmbeddingDeploymentName;
        o.ModelDeploymentName = openAiSettings.ModelDeploymentName;
    });

builder.Services.AddOpenTelemetry().UseAzureMonitor();
builder.Services
    .AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("KinHub")!,
        name: "kinhub-dev-psqldb",
        timeout: TimeSpan.FromSeconds(10));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = effectiveJwtIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(effectiveJwtSecret)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtAuthenticationMiddleware>();
builder.Services.AddSingleton<McpSessionStore>();
builder.Services.AddSingleton<IMcpSessionService, McpSessionService>();
builder.Services.AddSingleton<McpRequestValidator>();
builder.Services.AddScoped<IMcpDispatcher, McpDispatcher>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
    options.AddPolicy(CorsOptions.PolicyName, policy =>
    {
        if (corsOptions.AllowAnyOrigin || corsOptions.AllowedOrigins.Length is 0)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(corsOptions.AllowedOrigins);
        }

        policy.AllowAnyMethod()
              .AllowAnyHeader();
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(CorsOptions.PolicyName);
app.UseAuthentication();
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();