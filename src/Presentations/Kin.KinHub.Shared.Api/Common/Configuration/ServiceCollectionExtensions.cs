using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Kin.KinHub.Shared.Api.Common.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

namespace Kin.KinHub.Shared.Api.Common.Configuration;

public static class ServiceCollectionExtensions
{
    private const string DevelopmentJwtSecret = "development-only-kinhub-jwt-secret-0001";

    public static IServiceCollection AddKinHubSharedApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new();
        var openAiSettings = configuration.GetSection(OpenAiSettings.SectionName).Get<OpenAiSettings>() ?? new();
        var oauthOptions = configuration.GetSection(OAuthServerOptions.SectionName).Get<OAuthServerOptions>() ?? new();
        ValidateSecuritySettings(corsOptions, jwtSettings, oauthOptions, environment);
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;
        var effectiveJwtSecret = ResolveJwtSecret(jwtSettings.Secret, environment);
        var effectiveJwtIssuer = ResolveJwtIssuer(jwtSettings.Issuer, environment);

        services.AddSingleton(corsOptions);
        services.AddSingleton(oauthOptions);

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
            .AddKinHubIdentityBusiness()
            .AddKinHubCoreOpenAiInfrastructure(o =>
            {
                o.Endpoint = openAiSettings.Endpoint;
                o.ApiKey = openAiSettings.ApiKey;
                o.EmbeddingDeploymentName = openAiSettings.EmbeddingDeploymentName;
                o.ModelDeploymentName = openAiSettings.ModelDeploymentName;
            });

        AddAzureMonitorIfConfigured(services, configuration);
        services.AddHealthChecks().AddNpgSql(
            connectionString,
            name: "kinhub-dev-psqldb",
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

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        // Emit a uniform RFC 9457 problem detail instead of the default empty 401 body.
                        context.HandleResponse();
                        return ApiProblemDetails.WriteAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "authentication_required",
                            "Missing or invalid Authorization header.");
                    },
                    OnForbidden = context => ApiProblemDetails.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        "forbidden",
                        "You do not have access to this resource."),
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                FamilyContextRequirement.PolicyName,
                policy => policy.Requirements.Add(new FamilyContextRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, FamilyContextAuthorizationHandler>();
        services.AddScoped<IAuthorizationMiddlewareResultHandler, FamilyAuthorizationMiddlewareResultHandler>();
        services.AddHttpContextAccessor();
        services.AddScoped<JwtAuthenticationMiddleware>();
        services.AddSingleton<IOAuthClientStore>(_ => new InMemoryOAuthClientStore(oauthOptions));
        services.AddSingleton<IOAuthAuthorizationCodeStore>(_ => new InMemoryOAuthAuthorizationCodeStore(oauthOptions));
        services.AddSingleton<IOAuthRefreshTokenScopeStore>(_ => new InMemoryOAuthRefreshTokenScopeStore(oauthOptions));
        services.AddSingleton<IOAuthIdentitySessionStore>(_ => new InMemoryOAuthIdentitySessionStore(oauthOptions));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                OAuthServerOptions.RateLimitPolicyName,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"{context.Connection.RemoteIpAddress}:{context.Request.Path.Value}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = oauthOptions.RateLimitPermitLimit,
                        Window = TimeSpan.FromSeconds(oauthOptions.RateLimitWindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
        });

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

                    policy.AllowAnyMethod()
                          .AllowAnyHeader();
                });
        });

        return services;
    }

    private static void ValidateSecuritySettings(
        CorsOptions corsOptions,
        JwtSettings jwtSettings,
        OAuthServerOptions oauthOptions,
        IHostEnvironment environment)
    {
        if (oauthOptions.MaxRegisteredClients <= 0
            || oauthOptions.MaxAuthorizationCodes <= 0
            || oauthOptions.MaxScopedRefreshTokens <= 0
            || oauthOptions.MaxIdentitySessions <= 0
            || oauthOptions.SessionLifetimeHours <= 0
            || oauthOptions.RateLimitPermitLimit <= 0
            || oauthOptions.RateLimitWindowSeconds <= 0)
        {
            throw new InvalidOperationException("OAuth capacity and rate limit settings must be greater than zero.");
        }

        ValidateScopeSettings(oauthOptions);

        if (environment.IsDevelopment())
        {
            return;
        }

        var secret = jwtSettings.Secret?.Trim();
        if (string.IsNullOrWhiteSpace(secret)
            || secret.Length < 32
            || secret.StartsWith("CHANGE-ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Jwt:Secret must be configured with a random secret at least 32 characters long.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer must be configured.");
        }

        if (!Uri.TryCreate(oauthOptions.AuthorizationServerUrl, UriKind.Absolute, out var authorizationServerUri)
            || !string.Equals(authorizationServerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OAuth:AuthorizationServerUrl must be an absolute HTTPS URL outside development.");
        }

        ValidateCorsSettings("Cors", corsOptions.AllowAnyOrigin, corsOptions.AllowedOrigins);
    }

    private static void ValidateCorsSettings(string sectionName, bool allowAnyOrigin, string[] allowedOrigins)
    {
        if (allowAnyOrigin)
        {
            throw new InvalidOperationException($"{sectionName}:AllowAnyOrigin cannot be enabled outside development.");
        }

        if (allowedOrigins.Length is 0)
        {
            throw new InvalidOperationException($"{sectionName}:AllowedOrigins must contain at least one origin outside development.");
        }
    }

    private static void ValidateScopeSettings(OAuthServerOptions oauthOptions)
    {
        if (oauthOptions.SupportedScopes.Length is 0)
        {
            throw new InvalidOperationException("OAuth:SupportedScopes must contain at least one scope.");
        }

        ValidateScopeSubset("OAuth:DynamicClientDefaultScopes", oauthOptions.DynamicClientDefaultScopes, oauthOptions.DynamicClientAllowedScopes);
        ValidateScopeSubset("OAuth:DynamicClientAllowedScopes", oauthOptions.DynamicClientAllowedScopes, oauthOptions.SupportedScopes);
        ValidateScopeSubset("OAuth:ElevatedConsentScopes", oauthOptions.ElevatedConsentScopes, oauthOptions.SupportedScopes);
        ValidateRegisteredClients(oauthOptions);
    }

    private static void ValidateRegisteredClients(OAuthServerOptions oauthOptions)
    {
        var clientIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var client in oauthOptions.Clients)
        {
            if (string.IsNullOrWhiteSpace(client.ClientId))
            {
                throw new InvalidOperationException("OAuth:Clients entries must define ClientId.");
            }

            if (!clientIds.Add(client.ClientId.Trim()))
            {
                throw new InvalidOperationException($"OAuth:Clients contains duplicate ClientId '{client.ClientId}'.");
            }

            if (client.RedirectUris.Length is 0)
            {
                throw new InvalidOperationException($"OAuth client '{client.ClientId}' must define at least one redirect URI.");
            }

            if (client.RedirectUris.Any(uri => !Uri.TryCreate(uri, UriKind.Absolute, out _)))
            {
                throw new InvalidOperationException($"OAuth client '{client.ClientId}' contains an invalid redirect URI.");
            }

            ValidateScopeSubset($"OAuth:Clients:{client.ClientId}:Scope", SplitScope(client.Scope), oauthOptions.SupportedScopes);
        }
    }

    private static string[] SplitScope(string? scope) =>
        string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void ValidateScopeSubset(string settingName, string[] candidateScopes, string[] allowedScopes)
    {
        var normalizedCandidates = candidateScopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var normalizedAllowed = allowedScopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        if (normalizedCandidates.Length is 0)
        {
            throw new InvalidOperationException($"{settingName} must contain at least one scope.");
        }

        if (normalizedCandidates.Except(normalizedAllowed, StringComparer.Ordinal).Any())
        {
            throw new InvalidOperationException($"{settingName} must be a subset of the supported OAuth scopes.");
        }
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
