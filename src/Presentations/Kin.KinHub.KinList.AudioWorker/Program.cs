using Azure.Monitor.OpenTelemetry.AspNetCore;
using Kin.KinHub.KinList.AzureOpenAi.Common;
using Kin.KinHub.KinList.AudioWorker;
using Kin.KinHub.KinList.AzureStorage;
using Kin.KinHub.KinList.Business.Common;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "KINHUB_");

var kinListOptions = builder.Configuration.GetSection(KinListOptions.SectionName).Get<KinListOptions>() ?? new();
var speechOptions = builder.Configuration.GetSection(SpeechToTextOptions.SectionName).Get<SpeechToTextOptions>() ?? new();
var openAiOptions = builder.Configuration.GetSection("OpenAi").Get<OpenAiOptions>() ?? new();
var storageOptions = builder.Configuration.GetSection(AudioStorageOptions.SectionName).Get<AudioStorageOptions>() ?? new();
var connectionString = builder.Configuration.GetConnectionString("KinHub") ?? string.Empty;

kinListOptions.Validate();
storageOptions.Validate();

builder.Services
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
    })
    .AddKinHubKinListPostgreSqlInfrastructure(o =>
    {
        o.ConnectionString = connectionString;
    })
    .AddKinHubKinListAzureOpenAiInfrastructure(
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
        })
    .AddKinHubKinListAzureStorageInfrastructure(o =>
    {
        o.BlobServiceUri = storageOptions.BlobServiceUri;
        o.QueueServiceUri = storageOptions.QueueServiceUri;
        o.ContainerName = storageOptions.ContainerName;
        o.ProcessingQueueName = storageOptions.ProcessingQueueName;
        o.PoisonQueueName = storageOptions.PoisonQueueName;
    });

var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]?.Trim()
    ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]?.Trim();
if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
    {
        options.ConnectionString = aiConnectionString;
    });
}

builder.Services.AddSingleton<AudioQueueMessageProcessor>();
builder.Services.AddHostedService<AudioProcessingWorkerService>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    KinListTransactionExecutorGuard.EnsureConfigured(
        scope.ServiceProvider.GetRequiredService<IKinListTransactionExecutor>(),
        builder.Environment.IsDevelopment());
}

await host.RunAsync();
