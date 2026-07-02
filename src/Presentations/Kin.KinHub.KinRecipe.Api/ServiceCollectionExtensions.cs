using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Kin.KinHub.KinRecipe.Api.Common;
using Kin.KinHub.KinRecipe.Api.Common.Configuration;
using Kin.KinHub.Shared.Api.Common.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const string DevelopmentJwtSecret = "development-only-kinhub-jwt-secret-0001";

    public static IServiceCollection AddKinHubKinRecipeApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new();
        var openAiSettings = configuration.GetSection(OpenAiSettings.SectionName).Get<OpenAiSettings>() ?? new();
        var familyContextApiOptions = configuration.GetSection(FamilyContextApiOptions.SectionName).Get<FamilyContextApiOptions>() ?? new();
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;
        var effectiveJwtSecret = ResolveJwtSecret(jwtSettings.Secret, environment);
        var effectiveJwtIssuer = ResolveJwtIssuer(jwtSettings.Issuer, environment);
        familyContextApiOptions.Validate();

        services.AddSingleton(corsOptions);
        services.AddSingleton(familyContextApiOptions);

        services
            .AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped, includeInternalTypes: true)
            .AddScoped(typeof(IRequestValidator<>), typeof(FluentRequestValidator<>))
            .AddKinHubCorePostgreSqlInfrastructure(o =>
            {
                o.ConnectionString = connectionString;
            })
            .AddKinHubIdentityJwtInfrastructure(o =>
            {
                o.Secret = effectiveJwtSecret;
                o.AccessTokenExpiryMinutes = jwtSettings.AccessTokenExpiryMinutes;
                o.RefreshTokenExpiryDays = jwtSettings.RefreshTokenExpiryDays;
                o.Issuer = effectiveJwtIssuer;
                o.Audience = jwtSettings.Audience;
            })
            .AddKinHubCoreBusiness()
            .AddKinHubCoreOpenAiInfrastructure(o =>
            {
                o.Endpoint = openAiSettings.Endpoint;
                o.ApiKey = openAiSettings.ApiKey;
                o.EmbeddingDeploymentName = openAiSettings.EmbeddingDeploymentName;
                o.ModelDeploymentName = openAiSettings.ModelDeploymentName;
            });

        services.RemoveAll<IFamilyOwnershipService>();
        services.AddHttpClient<IFamilyOwnershipService, RemoteFamilyOwnershipService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<FamilyContextApiOptions>();
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        AddAzureMonitorIfConfigured(services, configuration);
        services.AddHealthChecks().AddNpgSql(
            connectionString,
            name: "kinhub-kinrecipe-db",
            timeout: TimeSpan.FromSeconds(10),
            tags: ["ready"]);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = effectiveJwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(effectiveJwtSecret)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireAssertion(HasApiScope)
                .Build();
            options.AddPolicy(
                FamilyContextRequirement.PolicyName,
                policy =>
                {
                    policy.RequireAssertion(HasApiScope);
                    policy.Requirements.Add(new FamilyContextRequirement());
                });
        });
        services.AddScoped<IAuthorizationHandler, FamilyContextAuthorizationHandler>();
        services.AddScoped<IAuthorizationMiddlewareResultHandler, FamilyAuthorizationMiddlewareResultHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<JwtAuthenticationMiddleware>();
        services.AddControllers();
        services.AddOpenApi();
        services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                if (corsOptions.AllowAnyOrigin || corsOptions.AllowedOrigins.Length is 0)
                {
                    policy.AllowAnyOrigin();
                }
                else
                {
                    policy.SetIsOriginAllowed(origin => corsOptions.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase));
                }

                policy.AllowAnyMethod().AllowAnyHeader();
            });
        });

        return services;
    }

    private static bool HasApiScope(AuthorizationHandlerContext context) =>
        context.User.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(OAuthScopes.Read, StringComparer.Ordinal);

    private static string ResolveJwtSecret(string? configuredSecret, IHostEnvironment environment)
    {
        var secret = configuredSecret?.Trim();
        if (!string.IsNullOrWhiteSpace(secret))
        {
            return secret;
        }

        if (environment.IsDevelopment())
        {
            return DevelopmentJwtSecret;
        }

        throw new InvalidOperationException("Jwt:Secret must be configured.");
    }

    private static string ResolveJwtIssuer(string? configuredIssuer, IHostEnvironment environment)
    {
        var issuer = configuredIssuer?.Trim();
        if (!string.IsNullOrWhiteSpace(issuer))
        {
            return issuer;
        }

        if (environment.IsDevelopment())
        {
            return "kinhub-development";
        }

        throw new InvalidOperationException("Jwt:Issuer must be configured.");
    }

    private static void AddAzureMonitorIfConfigured(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveAzureMonitorConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        services.AddOpenTelemetry().UseAzureMonitor(options =>
        {
            options.ConnectionString = connectionString;
        });
    }

    private static string? ResolveAzureMonitorConnectionString(IConfiguration configuration)
    {
        var configuredConnectionString = configuration["ApplicationInsights:ConnectionString"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        return configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]?.Trim();
    }
}
