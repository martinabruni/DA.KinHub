using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text;

namespace Kin.KinHub.Shared.Api.Common.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubSharedApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new();
        var openAiSettings = configuration.GetSection(OpenAiSettings.SectionName).Get<OpenAiSettings>() ?? new();
        var mcpOptions = configuration.GetSection(McpTransportOptions.SectionName).Get<McpTransportOptions>() ?? new();

        var effectiveJwtSecret = string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32
            ? "CHANGE-ME-use-a-long-random-secret-at-least-32-chars!"
            : jwtSettings.Secret;
        var effectiveJwtIssuer = string.IsNullOrWhiteSpace(jwtSettings.Issuer)
            ? "kinhub"
            : jwtSettings.Issuer;

        services.AddSingleton(corsOptions);
        services.AddSingleton(mcpOptions);

        services
            .AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped, includeInternalTypes: true)
            .AddScoped(typeof(IRequestValidator<>), typeof(FluentRequestValidator<>))
            .AddKinHubCorePostgreSqlInfrastructure(o => o.ConnectionString = configuration.GetConnectionString("KinHub")!)
            .AddKinHubIdentityPostgreSqlInfrastructure(o => o.ConnectionString = configuration.GetConnectionString("KinHub")!)
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
        services
            .AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("KinHub")!,
                name: "kinhub-dev-psqldb",
                timeout: TimeSpan.FromSeconds(10));

        services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
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

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<JwtAuthenticationMiddleware>();
        services.AddSingleton<IOAuthClientStore, InMemoryOAuthClientStore>();
        services.AddSingleton<IOAuthAuthorizationCodeStore, InMemoryOAuthAuthorizationCodeStore>();

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
                        policy.WithOrigins(corsOptions.AllowedOrigins);
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
                    policy.WithOrigins(mcpOptions.AllowedOrigins);
                }

                policy.WithMethods("POST")
                      .WithHeaders("Content-Type", "Authorization", "MCP-Protocol-Version");
                policy.WithExposedHeaders("WWW-Authenticate");
            });
        });

        return services;
    }
}
