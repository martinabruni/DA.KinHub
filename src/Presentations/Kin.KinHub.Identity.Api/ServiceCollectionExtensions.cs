using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Kin.KinHub.Shared.Api.Common.Authorization;
using Kin.KinHub.Identity.Api.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

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
        var oauthOptions = configuration.GetSection(OAuthServerOptions.SectionName).Get<OAuthServerOptions>() ?? new();
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;
        var effectiveJwtSecret = ResolveJwtSecret(jwtSettings.Secret, environment);
        var effectiveJwtIssuer = ResolveJwtIssuer(jwtSettings.Issuer, environment);
        ValidateProductionSecurity(corsOptions, jwtSettings, oauthOptions, environment);

        services.AddSingleton(corsOptions);
        services.AddSingleton(oauthOptions);
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

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
                o.Audience = jwtSettings.Audience;
            })
            .AddKinHubFamilyBusiness()
            .AddKinHubIdentityBusiness();

        AddAzureMonitorIfConfigured(services, configuration);
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
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(effectiveJwtSecret)),
                    ClockSkew = TimeSpan.Zero,
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
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
        services.AddScoped<IFamilyContextResolver, IdentityFamilyContextResolver>();
        services.AddSingleton<IOAuthClientStore>(_ => new InMemoryOAuthClientStore(oauthOptions));
        services.AddSingleton<IOAuthRefreshTokenScopeStore>(_ => new InMemoryOAuthRefreshTokenScopeStore(oauthOptions));
        if (environment.IsDevelopment())
        {
            services.AddSingleton<IOAuthAuthorizationCodeStore>(_ => new InMemoryOAuthAuthorizationCodeStore(oauthOptions));
            services.AddSingleton<IOAuthIdentitySessionStore>(_ => new InMemoryOAuthIdentitySessionStore(oauthOptions));
        }
        else
        {
            services.AddSingleton<IOAuthAuthorizationCodeStore>(
                _ => new PostgreSqlOAuthAuthorizationCodeStore(connectionString));
            services.AddSingleton<IOAuthIdentitySessionStore>(
                _ => new PostgreSqlOAuthIdentitySessionStore(connectionString));
        }
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                OAuthServerOptions.RateLimitPolicyName,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    $"{context.Connection.RemoteIpAddress}:{context.Request.Path}",
                    _ => new FixedWindowRateLimiterOptions
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

    private static void ValidateProductionSecurity(
        CorsOptions cors,
        JwtSettings jwt,
        OAuthServerOptions oauth,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment()) return;
        if (cors.AllowAnyOrigin || cors.AllowedOrigins.Length == 0)
            throw new InvalidOperationException("Cors must contain explicit origins outside development.");
        if (jwt.Secret.Trim().Length < 32 || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
            throw new InvalidOperationException("Jwt secret, issuer, and audience must be configured securely.");
        if (!Uri.TryCreate(oauth.AuthorizationServerUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("OAuth:AuthorizationServerUrl must use HTTPS outside development.");
        if (oauth.Clients.Length != 4)
            throw new InvalidOperationException("OAuth must configure exactly the four KinHub SPA clients.");
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
