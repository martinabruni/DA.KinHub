using Azure.Monitor.OpenTelemetry.AspNetCore;
using FluentValidation;
using Kin.KinHub.App.Functions.Common;
using Kin.KinHub.App.Functions.Common.Authorization;
using Kin.KinHub.App.Functions.Common.Configuration;
using Kin.KinHub.App.Functions.Common.Validators;
using Kin.KinHub.Core.Business.FamilyFeature;
using Kin.KinHub.KinList.AzureOpenAi.Common;
using Kin.KinHub.KinList.AzureStorage;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.Shared.Kernel.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const string DevelopmentJwtSecret = "development-only-kinhub-jwt-secret-0001";

    public static IServiceCollection AddKinHubAppFunctions(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new();
        var effectiveJwtSecret = ResolveJwtSecret(jwtSettings.Secret, environment);
        var effectiveJwtIssuer = ResolveJwtIssuer(jwtSettings.Issuer, environment);
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;

        services
            .AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped, includeInternalTypes: true)
            .AddScoped(typeof(IRequestValidator<>), typeof(FluentRequestValidator<>))
            .AddKinHubIdentityJwtInfrastructure(o =>
            {
                o.Secret = effectiveJwtSecret;
                o.AccessTokenExpiryMinutes = jwtSettings.AccessTokenExpiryMinutes;
                o.RefreshTokenExpiryDays = jwtSettings.RefreshTokenExpiryDays;
                o.Issuer = effectiveJwtIssuer;
                o.Audience = jwtSettings.Audience;
            })
            .AddKinHubCorePostgreSqlInfrastructure(o =>
            {
                o.ConnectionString = connectionString;
            })
            .AddKinHubFamilyBusiness()
            .AddKinHubKinListInfrastructure(configuration, environment)
            .AddKinHubKinRecipeInfrastructure(configuration, environment)
            .AddHttpContextAccessor()
            .AddScoped<IFamilyContextResolver, CoreFamilyContextResolver>()
            .AddScoped<FunctionsAuthorizationService>()
            .AddSingleton<IAudioProcessingQueueConsumer, AudioProcessingQueueConsumer>();

        AddAzureMonitorIfConfigured(services, configuration);

        return services;
    }

    private static IServiceCollection AddKinHubKinListInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new();
        var kinListOptions = configuration.GetSection(KinListOptions.SectionName).Get<KinListOptions>() ?? new();
        var audioStorageOptions = configuration.GetSection(AudioStorageOptions.SectionName).Get<AudioStorageOptions>() ?? new();
        var speechOptions = configuration.GetSection(SpeechToTextOptions.SectionName).Get<SpeechToTextOptions>() ?? new();
        var openAiOptions = configuration.GetSection("OpenAi").Get<OpenAiOptions>() ?? new();
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;
        ValidateProductionSecurity(corsOptions, jwtSettings, environment);
        kinListOptions.Validate();

        services.AddSingleton(corsOptions);
        services.AddSingleton(kinListOptions);

        services
            .AddKinHubKinListPostgreSqlInfrastructure(o =>
            {
                o.ConnectionString = connectionString;
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

        services.AddHostedService<IdempotencyRecordCleanupService>();

        return services;
    }

    private static IServiceCollection AddKinHubKinRecipeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new();
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new();
        var openAiSettings = configuration.GetSection(OpenAiSettings.SectionName).Get<OpenAiSettings>() ?? new();
        var connectionString = configuration.GetConnectionString("KinHub") ?? string.Empty;
        ValidateProductionSecurity(corsOptions, jwtSettings, environment);

        services.AddSingleton(corsOptions);

        services
            .AddKinHubKinRecipePostgreSqlInfrastructure(o =>
            {
                o.ConnectionString = connectionString;
            })
            .AddKinHubKinRecipeBusiness()
            .AddKinHubKinRecipeAzureOpenAiInfrastructure(o =>
            {
                o.Endpoint = openAiSettings.Endpoint;
                o.ApiKey = openAiSettings.ApiKey;
                o.EmbeddingDeploymentName = openAiSettings.EmbeddingDeploymentName;
                o.ModelDeploymentName = openAiSettings.ModelDeploymentName;
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

    private static void ValidateProductionSecurity(
        CorsOptions cors,
        JwtSettings jwt,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment()) return;
        if (cors.AllowAnyOrigin || cors.AllowedOrigins.Length == 0)
            throw new InvalidOperationException("Cors must contain explicit origins outside development.");
        if (jwt.Secret.Trim().Length < 32 || string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience))
            throw new InvalidOperationException("Jwt secret, issuer, and audience must be configured securely.");
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
