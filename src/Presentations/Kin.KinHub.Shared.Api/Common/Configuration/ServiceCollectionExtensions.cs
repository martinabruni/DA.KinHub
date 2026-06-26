using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
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
        var mcpOptions = configuration.GetSection(McpTransportOptions.SectionName).Get<McpTransportOptions>() ?? new();
        var skipPostgreSqlConnectionValidation = configuration.GetValue<bool>("Testing:SkipPostgreSqlConnectionValidation");
        ValidateSecuritySettings(corsOptions, jwtSettings, mcpOptions, environment);
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;
        var effectiveJwtSecret = ResolveJwtSecret(jwtSettings.Secret, environment);
        var effectiveJwtIssuer = ResolveJwtIssuer(jwtSettings.Issuer, environment);

        services.AddSingleton(corsOptions);
        services.AddSingleton(mcpOptions);

        services
            .AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped, includeInternalTypes: true)
            .AddScoped(typeof(IRequestValidator<>), typeof(FluentRequestValidator<>))
            .AddKinHubCorePostgreSqlInfrastructure(o =>
            {
                o.ConnectionString = connectionString;
                o.SkipConnectionStringValidation = skipPostgreSqlConnectionValidation;
            })
            .AddKinHubIdentityPostgreSqlInfrastructure(o =>
            {
                o.ConnectionString = connectionString;
                o.SkipConnectionStringValidation = skipPostgreSqlConnectionValidation;
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

        services.AddOpenTelemetry().UseAzureMonitor();
        var healthChecks = services.AddHealthChecks();
        if (!skipPostgreSqlConnectionValidation)
        {
            healthChecks.AddNpgSql(
                connectionString,
                name: "kinhub-dev-psqldb",
                timeout: TimeSpan.FromSeconds(10),
                tags: ["ready"]);
        }

        services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
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
            })
            .AddMcp(options =>
            {
                options.ResourceMetadata = new ProtectedResourceMetadata
                {
                    ResourceName = mcpOptions.ResourceName,
                    ResourceDocumentation = mcpOptions.ResourceDocumentation,
                    AuthorizationServers = { mcpOptions.AuthorizationServerUrl },
                    ScopesSupported = [.. mcpOptions.SupportedScopes],
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                McpAuthorizationPolicies.Read,
                policy => policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => McpAuthorizationPolicies.HasAnyScope(context.User, McpScopes.Read, McpScopes.Write, McpScopes.Admin)));
            options.AddPolicy(
                McpAuthorizationPolicies.Write,
                policy => policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => McpAuthorizationPolicies.HasAnyScope(context.User, McpScopes.Write, McpScopes.Admin)));
            options.AddPolicy(
                McpAuthorizationPolicies.Admin,
                policy => policy.RequireAuthenticatedUser()
                    .RequireAssertion(context => McpAuthorizationPolicies.HasAnyScope(context.User, McpScopes.Admin)));
        });
        services.AddHttpContextAccessor();
        services.AddScoped<JwtAuthenticationMiddleware>();
        services.AddSingleton<IOAuthClientStore>(_ => new InMemoryOAuthClientStore(mcpOptions));
        services.AddSingleton<IOAuthAuthorizationCodeStore>(_ => new InMemoryOAuthAuthorizationCodeStore(mcpOptions));
        services.AddSingleton<IOAuthRefreshTokenScopeStore>(_ => new InMemoryOAuthRefreshTokenScopeStore(mcpOptions));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                McpTransportOptions.OAuthRateLimitPolicyName,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"{context.Connection.RemoteIpAddress}:{context.Request.Path.Value}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = mcpOptions.OAuthRateLimitPermitLimit,
                        Window = TimeSpan.FromSeconds(mcpOptions.OAuthRateLimitWindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
        });

        services
            .AddMcpServer(options =>
            {
                options.ProtocolVersion = mcpOptions.ProtocolVersion;
                options.ServerInfo = new Implementation
                {
                    Name = mcpOptions.ServerName,
                    Version = mcpOptions.ServerVersion,
                };
                options.ServerInstructions = mcpOptions.Instructions;
            })
            .WithHttpTransport(options =>
            {
                options.Stateless = true;
            })
            .AddAuthorizationFilters()
            .WithTools<AuthenticationMcpTools>()
            .WithTools<FamilyMcpTools>()
            .WithTools<RecipeMcpTools>()
            .WithTools<RecipeAssistantMcpTools>();

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

            options.AddPolicy(McpTransportOptions.CorsPolicyName, policy =>
            {
                if (mcpOptions.AllowAnyOrigin || mcpOptions.AllowedOrigins.Length is 0)
                {
                    policy.AllowAnyOrigin();
                }
                else
                {
                    policy.SetIsOriginAllowed(origin => mcpOptions.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase));
                }

                policy.WithMethods("POST")
                      .WithHeaders("Content-Type", "Authorization", "MCP-Protocol-Version");
                policy.WithExposedHeaders("WWW-Authenticate");
            });
        });

        return services;
    }

    private static void ValidateSecuritySettings(
        CorsOptions corsOptions,
        JwtSettings jwtSettings,
        McpTransportOptions mcpOptions,
        IHostEnvironment environment)
    {
        if (mcpOptions.MaxRegisteredClients <= 0
            || mcpOptions.MaxAuthorizationCodes <= 0
            || mcpOptions.MaxScopedRefreshTokens <= 0
            || mcpOptions.OAuthRateLimitPermitLimit <= 0
            || mcpOptions.OAuthRateLimitWindowSeconds <= 0)
        {
            throw new InvalidOperationException("Mcp capacity and rate limit settings must be greater than zero.");
        }

        ValidateScopeSettings(mcpOptions);

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

        if (!Uri.TryCreate(mcpOptions.AuthorizationServerUrl, UriKind.Absolute, out var authorizationServerUri)
            || !string.Equals(authorizationServerUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Mcp:AuthorizationServerUrl must be an absolute HTTPS URL outside development.");
        }

        ValidateCorsSettings("Cors", corsOptions.AllowAnyOrigin, corsOptions.AllowedOrigins);
        ValidateCorsSettings("Mcp", mcpOptions.AllowAnyOrigin, mcpOptions.AllowedOrigins);
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

    private static void ValidateScopeSettings(McpTransportOptions mcpOptions)
    {
        if (mcpOptions.SupportedScopes.Length is 0)
        {
            throw new InvalidOperationException("Mcp:SupportedScopes must contain at least one scope.");
        }

        ValidateScopeSubset("Mcp:DynamicClientDefaultScopes", mcpOptions.DynamicClientDefaultScopes, mcpOptions.DynamicClientAllowedScopes);
        ValidateScopeSubset("Mcp:DynamicClientAllowedScopes", mcpOptions.DynamicClientAllowedScopes, mcpOptions.SupportedScopes);
        ValidateScopeSubset("Mcp:ElevatedConsentScopes", mcpOptions.ElevatedConsentScopes, mcpOptions.SupportedScopes);
    }

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
            throw new InvalidOperationException($"{settingName} must be a subset of the supported MCP scopes.");
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
}
