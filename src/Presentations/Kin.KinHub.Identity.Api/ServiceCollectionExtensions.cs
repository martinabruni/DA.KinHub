using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const string DevelopmentJwtSecret = "development-only-kinhub-jwt-secret-0001";

    public static IServiceCollection AddKinHubIdentityApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new();
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;
        var effectiveJwtSecret = ResolveJwtSecret(jwtSettings.Secret, environment);
        var effectiveJwtIssuer = ResolveJwtIssuer(jwtSettings.Issuer, environment);

        services.AddSingleton(corsOptions);

        services
            .AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped, includeInternalTypes: true)
            .AddScoped(typeof(IRequestValidator<>), typeof(FluentRequestValidator<>))
            .AddKinHubCorePostgreSqlInfrastructure(o =>
            {
                o.ConnectionString = connectionString;
            })
            .AddKinHubIdentityPostgreSqlInfrastructure(o =>
            {
                o.ConnectionString = connectionString;
            })
            .AddKinHubIdentityJwtInfrastructure(o =>
            {
                o.Secret = effectiveJwtSecret;
                o.AccessTokenExpiryMinutes = jwtSettings.AccessTokenExpiryMinutes;
                o.RefreshTokenExpiryDays = jwtSettings.RefreshTokenExpiryDays;
                o.Issuer = effectiveJwtIssuer;
            })
            .AddKinHubCoreBusiness()
            .AddKinHubIdentityBusiness();

        services.AddOpenTelemetry().UseAzureMonitor();
        services.AddHealthChecks().AddNpgSql(
            connectionString,
            name: "kinhub-identity-db",
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
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(effectiveJwtSecret)),
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization();
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
}
