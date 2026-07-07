using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Kin.KinHub.KinList.AzureOpenAi.Common;
using Kin.KinHub.KinList.AzureStorage;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Api.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Kin.KinHub.Core.Api.Common.Authorization;
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
        var familyContextApiOptions = configuration.GetSection(FamilyContextApiOptions.SectionName).Get<FamilyContextApiOptions>() ?? new();
        var kinListOptions = configuration.GetSection(KinListOptions.SectionName).Get<KinListOptions>() ?? new();
        var audioStorageOptions = configuration.GetSection(AudioStorageOptions.SectionName).Get<AudioStorageOptions>() ?? new();
        var speechOptions = configuration.GetSection(SpeechToTextOptions.SectionName).Get<SpeechToTextOptions>() ?? new();
        var openAiOptions = configuration.GetSection("OpenAi").Get<OpenAiOptions>() ?? new();
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;
        var effectiveJwtSecret = ResolveJwtSecret(jwtSettings.Secret, environment);
        var effectiveJwtIssuer = ResolveJwtIssuer(jwtSettings.Issuer, environment);
        ValidateProductionSecurity(corsOptions, jwtSettings, familyContextApiOptions, environment);
        familyContextApiOptions.Validate();
        kinListOptions.Validate();

        services.AddSingleton(corsOptions);
        services.AddSingleton(familyContextApiOptions);
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
                o.Audience = jwtSettings.Audience;
            })
            .AddKinHubKinListBusiness(o =>
            {
                o.MaxTitleLength = kinListOptions.MaxTitleLength;
                o.MaxItemLength = kinListOptions.MaxItemLength;
                o.MaxItemsPerList = kinListOptions.MaxItemsPerList;
                o.MaxItemsPerBulkConfirm = kinListOptions.MaxItemsPerBulkConfirm;
                o.IdempotencyRetentionHours = kinListOptions.IdempotencyRetentionHours;
                o.MaxAudioDurationSeconds = kinListOptions.MaxAudioDurationSeconds;
                o.MaxAudioBytes = kinListOptions.MaxAudioBytes;
                o.AudioProcessingTimeoutSeconds = kinListOptions.AudioProcessingTimeoutSeconds;
                o.AudioUploadSasTtlMinutes = kinListOptions.AudioUploadSasTtlMinutes;
                o.AudioOperationRetentionHours = kinListOptions.AudioOperationRetentionHours;
                o.AudioPollingRetryAfterSeconds = kinListOptions.AudioPollingRetryAfterSeconds;
                o.AudioProcessingMaxDequeues = kinListOptions.AudioProcessingMaxDequeues;
                o.TransientRetryMaxAttempts = kinListOptions.TransientRetryMaxAttempts;
                o.TransientRetryBaseDelayMilliseconds = kinListOptions.TransientRetryBaseDelayMilliseconds;
                o.TransientRetryMaxDelayMilliseconds = kinListOptions.TransientRetryMaxDelayMilliseconds;
                o.IdempotencyCleanupIntervalMinutes = kinListOptions.IdempotencyCleanupIntervalMinutes;
                o.AllowedAudioMimeTypes = kinListOptions.AllowedAudioMimeTypes;
            });

        var hasAudioStorageConfiguration =
            !string.IsNullOrWhiteSpace(audioStorageOptions.BlobServiceUri)
            && !string.IsNullOrWhiteSpace(audioStorageOptions.QueueServiceUri);

        var hasPartialAudioStorageConfiguration =
            !string.IsNullOrWhiteSpace(audioStorageOptions.BlobServiceUri)
            || !string.IsNullOrWhiteSpace(audioStorageOptions.QueueServiceUri);

        if (hasPartialAudioStorageConfiguration && !hasAudioStorageConfiguration)
        {
            throw new InvalidOperationException("Audio processing storage requires both AudioStorage:BlobServiceUri and AudioStorage:QueueServiceUri.");
        }

        if (hasAudioStorageConfiguration)
        {
            services.AddKinHubKinListAzureStorageInfrastructure(o =>
            {
                o.BlobServiceUri = audioStorageOptions.BlobServiceUri;
                o.QueueServiceUri = audioStorageOptions.QueueServiceUri;
                o.ContainerName = audioStorageOptions.ContainerName;
                o.ProcessingQueueName = audioStorageOptions.ProcessingQueueName;
                o.PoisonQueueName = audioStorageOptions.PoisonQueueName;
            });
        }

        var hasConfiguredAudioPipeline = speechOptions.IsConfigured() && openAiOptions.IsConfigured();
        var hasPartialAudioPipelineConfiguration =
            speechOptions.HasPartialConfiguration()
            || openAiOptions.HasPartialConfiguration();

        if (hasPartialAudioPipelineConfiguration && !hasConfiguredAudioPipeline)
        {
            throw new InvalidOperationException("KinList audio processing requires both Speech and OpenAi endpoint/apiKey configuration.");
        }

        if (hasConfiguredAudioPipeline)
        {
            services.AddKinHubKinListAzureOpenAiInfrastructure(
                configureSpeech: o =>
                {
                    o.Endpoint = speechOptions.Endpoint;
                    o.ApiKey = speechOptions.ApiKey;
                    o.UseManagedIdentity = speechOptions.UseManagedIdentity;
                    o.CandidateLocales = speechOptions.CandidateLocales;
                },
                configureOpenAi: o =>
                {
                    o.Endpoint = openAiOptions.Endpoint;
                    o.ApiKey = openAiOptions.ApiKey;
                    o.UseManagedIdentity = openAiOptions.UseManagedIdentity;
                    o.ModelDeploymentName = openAiOptions.ModelDeploymentName;
                });
        }

        services.AddHttpClient<IFamilyContextResolver, RemoteFamilyContextResolver>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<FamilyContextApiOptions>();
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
        services.AddHostedService<IdempotencyRecordCleanupService>();
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
        FamilyContextApiOptions familyContextApi,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment()) return;
        if (cors.AllowAnyOrigin || cors.AllowedOrigins.Length == 0)
            throw new InvalidOperationException("Cors must contain explicit origins outside development.");
        if (jwt.Secret.Trim().Length < 32 || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
            throw new InvalidOperationException("Jwt secret, issuer, and audience must be configured securely.");
        if (!Uri.TryCreate(familyContextApi.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("FamilyContextApi:BaseUrl must use HTTPS outside development.");
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
