using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Api.Common;
using Kin.KinHub.KinList.Api.Common.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const string DevelopmentJwtSecret = "development-only-kinhub-jwt-secret-0001";

    public static IServiceCollection AddKinHubKinListApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new();
        var coreApiOptions = configuration.GetSection(CoreApiOptions.SectionName).Get<CoreApiOptions>() ?? new();
        var kinListOptions = configuration.GetSection(KinListOptions.SectionName).Get<KinListOptions>() ?? new();
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;
        var effectiveJwtSecret = ResolveJwtSecret(jwtSettings.Secret, environment);
        var effectiveJwtIssuer = ResolveJwtIssuer(jwtSettings.Issuer, environment);
        coreApiOptions.Validate();
        kinListOptions.Validate();

        services.AddSingleton(corsOptions);
        services.AddSingleton(coreApiOptions);
        services.AddSingleton(kinListOptions);

        services
            .AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped, includeInternalTypes: true)
            .AddScoped(typeof(IRequestValidator<>), typeof(FluentRequestValidator<>))
            .AddKinHubKinListPostgreSqlInfrastructure(o =>
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
            .AddKinHubKinListBusiness(o =>
            {
                o.MaxTitleLength = kinListOptions.MaxTitleLength;
                o.MaxItemLength = kinListOptions.MaxItemLength;
                o.MaxItemsPerList = kinListOptions.MaxItemsPerList;
                o.MaxItemsPerBulkConfirm = kinListOptions.MaxItemsPerBulkConfirm;
                o.IdempotencyRetentionHours = kinListOptions.IdempotencyRetentionHours;
                o.MaxAudioDurationSeconds = kinListOptions.MaxAudioDurationSeconds;
                o.MaxAudioBytes = kinListOptions.MaxAudioBytes;
                o.AllowedAudioMimeTypes = kinListOptions.AllowedAudioMimeTypes;
            });

        services.RemoveAll<IFamilyOwnershipService>();
        services.AddHttpClient<IFamilyOwnershipService, RemoteFamilyOwnershipService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<CoreApiOptions>();
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        AddAzureMonitorIfConfigured(services, configuration);
        services.AddHealthChecks().AddNpgSql(
            connectionString,
            name: "kinhub-kinlist-db",
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

    private static void AddAzureMonitorIfConfigured(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ApplicationInsights:ConnectionString"]?.Trim()
            ?? configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]?.Trim();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        services.AddOpenTelemetry().UseAzureMonitor(options =>
        {
            options.ConnectionString = connectionString;
        });
    }
}
