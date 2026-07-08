using Kin.KinHub.KinList.AudioWorker;
using Kin.KinHub.KinList.AzureStorage;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.KinList.Domain.KinListFeature;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Text.Json;

namespace Kin.KinHub.Core.Test;

public sealed class KinListAudioWorkerTests
{
    [Fact]
    public async Task ProcessMessageAsync_WhenPayloadIsInvalid_MovesMessageToPoisonAndDeletesIt()
    {
        var queuePump = new FakeAudioProcessingQueuePump();
        var worker = CreateWorker(queuePump, new FakeAudioOperationProcessor());
        var message = new AudioProcessingQueueMessage
        {
            MessageId = "m1",
            PopReceipt = "p1",
            MessageText = "not-json",
            DequeueCount = 1,
        };

        await worker.ProcessMessageAsync(message, CancellationToken.None);

        Assert.Single(queuePump.PoisonMessages);
        Assert.Equal("not-json", queuePump.PoisonMessages[0]);
        Assert.Single(queuePump.DeletedMessageIds);
        Assert.Equal("m1", queuePump.DeletedMessageIds[0]);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenContractVersionIsUnsupported_MovesMessageToPoisonAndDeletesIt()
    {
        var queuePump = new FakeAudioProcessingQueuePump();
        var worker = CreateWorker(queuePump, new FakeAudioOperationProcessor());
        var message = new AudioProcessingQueueMessage
        {
            MessageId = "m1b",
            PopReceipt = "p1b",
            MessageText = $$"""{"contractVersion":2,"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""",
            DequeueCount = 1,
        };

        await worker.ProcessMessageAsync(message, CancellationToken.None);

        Assert.Single(queuePump.PoisonMessages);
        Assert.Single(queuePump.DeletedMessageIds);
        Assert.Equal("m1b", queuePump.DeletedMessageIds[0]);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenDequeueLimitExceeded_MarksFailedAndMovesToPoison()
    {
        var processor = new FakeAudioOperationProcessor();
        var queuePump = new FakeAudioProcessingQueuePump();
        var worker = CreateWorker(queuePump, processor, new KinListOptions { AudioProcessingMaxDequeues = 5 });
        var operationId = Guid.NewGuid();
        var message = new AudioProcessingQueueMessage
        {
            MessageId = "m2",
            PopReceipt = "p2",
            MessageText = $$"""{"operationId":"{{operationId}}","correlationId":"corr"}""",
            DequeueCount = 6,
        };

        await worker.ProcessMessageAsync(message, CancellationToken.None);

        Assert.Single(processor.MarkFailedCalls);
        Assert.Equal(operationId, processor.MarkFailedCalls[0].OperationId);
        Assert.Single(queuePump.PoisonMessages);
        Assert.Single(queuePump.DeletedMessageIds);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenProcessingSucceeds_DeletesMessage()
    {
        var processor = new FakeAudioOperationProcessor
        {
            ProcessResult = Result<AudioProcessingOperationResponse>.Success(new AudioProcessingOperationResponse
            {
                Id = Guid.NewGuid(),
                Type = "NewList",
                Status = "Succeeded",
                RetryAfterSeconds = 2,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            }),
        };
        var queuePump = new FakeAudioProcessingQueuePump();
        var worker = CreateWorker(queuePump, processor);
        var message = new AudioProcessingQueueMessage
        {
            MessageId = "m3",
            PopReceipt = "p3",
            MessageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""",
            DequeueCount = 1,
        };

        await worker.ProcessMessageAsync(message, CancellationToken.None);

        Assert.Single(queuePump.DeletedMessageIds);
        Assert.Empty(queuePump.PoisonMessages);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenProcessingIsTransient_LeavesMessageForRetry()
    {
        var processor = new FakeAudioOperationProcessor
        {
            ProcessResult = Result<AudioProcessingOperationResponse>.ServiceUnavailable("temporarily unavailable", "audio_processing_unavailable"),
        };
        var queuePump = new FakeAudioProcessingQueuePump();
        var worker = CreateWorker(queuePump, processor);
        var message = new AudioProcessingQueueMessage
        {
            MessageId = "m4",
            PopReceipt = "p4",
            MessageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""",
            DequeueCount = 1,
        };

        await worker.ProcessMessageAsync(message, CancellationToken.None);

        Assert.Empty(queuePump.DeletedMessageIds);
        Assert.Empty(queuePump.PoisonMessages);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenProcessingIsLong_RenewsVisibility()
    {
        var processor = new FakeAudioOperationProcessor
        {
            ProcessDelay = TimeSpan.FromSeconds(16),
            ProcessResult = Result<AudioProcessingOperationResponse>.Success(new AudioProcessingOperationResponse
            {
                Id = Guid.NewGuid(),
                Type = "NewList",
                Status = "Succeeded",
                RetryAfterSeconds = 2,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
            }),
        };
        var queuePump = new FakeAudioProcessingQueuePump();
        var worker = CreateWorker(queuePump, processor);
        var message = new AudioProcessingQueueMessage
        {
            MessageId = "m5",
            PopReceipt = "p5",
            MessageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""",
            DequeueCount = 1,
        };

        await worker.ProcessMessageAsync(message, CancellationToken.None);

        Assert.NotEmpty(queuePump.RenewedMessageIds);
        Assert.Contains("m5", queuePump.RenewedMessageIds);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenProcessorThrows_LeavesMessageForRetry()
    {
        var processor = new FakeAudioOperationProcessor
        {
            ThrowOnProcess = new InvalidOperationException("boom"),
        };
        var queuePump = new FakeAudioProcessingQueuePump();
        var worker = CreateWorker(queuePump, processor);
        var message = new AudioProcessingQueueMessage
        {
            MessageId = "m6",
            PopReceipt = "p6",
            MessageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""",
            DequeueCount = 1,
        };

        await worker.ProcessMessageAsync(message, CancellationToken.None);

        Assert.Empty(queuePump.DeletedMessageIds);
        Assert.Empty(queuePump.PoisonMessages);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenCorrelationIdContainsTraceContext_UsesItAsParent()
    {
        using var parent = new Activity("test-parent");
        parent.SetIdFormat(ActivityIdFormat.W3C);
        parent.Start();

        var processor = new FakeAudioOperationProcessor();
        var queuePump = new FakeAudioProcessingQueuePump();
        var worker = CreateWorker(queuePump, processor);
        var message = new AudioProcessingQueueMessage
        {
            MessageId = "m7",
            PopReceipt = "p7",
            MessageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"{{parent.Id}}"}""",
            DequeueCount = 1,
        };

        await worker.ProcessMessageAsync(message, CancellationToken.None);

        Assert.Equal(parent.TraceId, processor.LastObservedTraceId);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenDeleteFailsAfterSuccessfulSave_RetryIsIdempotent()
    {
        var operationId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("D");
        var generator = new CountingAudioDraftGenerator();
        var blobStorage = new InMemoryAudioBlobStorage();
        blobStorage.Seed("family/op-1", [1, 2, 3, 4], "audio/webm");

        var store = new InMemoryKinListStore();
        await store.AddAsync(new AudioProcessingOperation
        {
            Id = operationId,
            FamilyId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = AudioProcessingOperationType.NewList,
            Status = AudioProcessingOperationStatus.Queued,
            BlobName = "family/op-1",
            ContentType = "audio/webm",
            DeclaredByteSize = 4,
            UploadedByteSize = 4,
            CorrelationId = correlationId,
            Version = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UploadCompletedAt = DateTime.UtcNow,
        });

        var queuePump = new FakeAudioProcessingQueuePump
        {
            DeleteFailuresRemaining = 1,
        };
        var worker = CreateWorkerWithRealService(queuePump, store, generator, blobStorage);
        var payload = JsonSerializer.Serialize(new { operationId, correlationId });
        var message = new AudioProcessingQueueMessage
        {
            MessageId = "m8",
            PopReceipt = "p8",
            MessageText = payload,
            DequeueCount = 1,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => worker.ProcessMessageAsync(message, CancellationToken.None));

        var afterFirstAttempt = await store.GetByIdAsync(operationId, CancellationToken.None);
        Assert.NotNull(afterFirstAttempt);
        Assert.Equal(AudioProcessingOperationStatus.Succeeded, afterFirstAttempt!.Status);
        Assert.Null(await blobStorage.GetBlobAsync("family/op-1", CancellationToken.None));
        Assert.Equal(1, generator.CallCount);

        await worker.ProcessMessageAsync(message, CancellationToken.None);

        var afterRetry = await store.GetByIdAsync(operationId, CancellationToken.None);
        Assert.NotNull(afterRetry);
        Assert.Equal(AudioProcessingOperationStatus.Succeeded, afterRetry!.Status);
        Assert.Equal(1, generator.CallCount);
        Assert.Equal(2, queuePump.DeleteAttempts);
        Assert.Single(queuePump.DeletedMessageIds);
        Assert.Equal("m8", queuePump.DeletedMessageIds[0]);
    }

    private static AudioProcessingWorkerService CreateWorker(
        FakeAudioProcessingQueuePump queuePump,
        FakeAudioOperationProcessor processor,
        KinListOptions? options = null)
    {
        options ??= new KinListOptions();

        var services = new ServiceCollection();
        services.AddSingleton<IAudioOperationProcessor>(processor);
        var provider = services.BuildServiceProvider();
        var messageProcessor = new AudioQueueMessageProcessor(
            queuePump,
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<AudioQueueMessageProcessor>.Instance);

        return new AudioProcessingWorkerService(
            queuePump,
            messageProcessor,
            options,
            NullLogger<AudioProcessingWorkerService>.Instance);
    }

    private static AudioProcessingWorkerService CreateWorkerWithRealService(
        FakeAudioProcessingQueuePump queuePump,
        InMemoryKinListStore store,
        IKinListAudioDraftGenerator audioDraftGenerator,
        InMemoryAudioBlobStorage blobStorage,
        KinListOptions? options = null)
    {
        options ??= new KinListOptions();

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton<IKinListRepository>(store);
        services.AddSingleton<IKinListItemRepository>(store);
        services.AddSingleton<IIdempotencyRecordRepository>(store);
        services.AddSingleton<IAudioProcessingOperationRepository>(store);
        services.AddSingleton<IKinListTransactionExecutor, TestKinListTransactionExecutor>();
        services.AddSingleton(audioDraftGenerator);
        services.AddSingleton<IKinListAudioDraftGenerator>(audioDraftGenerator);
        services.AddSingleton(blobStorage);
        services.AddSingleton<IAudioProcessingBlobStorage>(blobStorage);
        services.AddSingleton<IAudioProcessingQueue>(new InMemoryAudioProcessingQueue());
        services.AddSingleton<IKinListItemDeduplicator, KinListItemDeduplicator>();
        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
        services.AddSingleton<CreateAudioProcessingOperationBusinessValidator>();
        services.AddSingleton<ILogger<KinListAudioService>>(_ => NullLogger<KinListAudioService>.Instance);
        services.AddScoped<IAudioOperationProcessor, KinListAudioService>();
        var provider = services.BuildServiceProvider();
        var messageProcessor = new AudioQueueMessageProcessor(
            queuePump,
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<AudioQueueMessageProcessor>.Instance);

        return new AudioProcessingWorkerService(
            queuePump,
            messageProcessor,
            options,
            NullLogger<AudioProcessingWorkerService>.Instance);
    }
}

internal sealed class FakeAudioProcessingQueuePump : IAudioProcessingQueuePump
{
    public List<string> DeletedMessageIds { get; } = [];
    public List<string> PoisonMessages { get; } = [];
    public List<string> RenewedMessageIds { get; } = [];
    public int DeleteFailuresRemaining { get; set; }
    public int DeleteAttempts { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyList<AudioProcessingQueueMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AudioProcessingQueueMessage>>([]);

    public Task DeleteMessageAsync(AudioProcessingQueueMessage message, CancellationToken cancellationToken)
    {
        DeleteAttempts += 1;
        if (DeleteFailuresRemaining > 0)
        {
            DeleteFailuresRemaining -= 1;
            throw new InvalidOperationException("delete failed");
        }

        DeletedMessageIds.Add(message.MessageId);
        return Task.CompletedTask;
    }

    public Task SendPoisonMessageAsync(string payload, CancellationToken cancellationToken)
    {
        PoisonMessages.Add(payload);
        return Task.CompletedTask;
    }

    public Task RenewMessageVisibilityAsync(AudioProcessingQueueMessage message, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
    {
        RenewedMessageIds.Add(message.MessageId);
        message.PopReceipt = $"{message.PopReceipt}-renewed";
        return Task.CompletedTask;
    }
}

internal sealed class CountingAudioDraftGenerator : IKinListAudioDraftGenerator
{
    public int CallCount { get; private set; }

    public Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default)
    {
        CallCount += 1;
        return Task.FromResult(Result<ParsedKinListAudioDraft>.Success(new ParsedKinListAudioDraft
        {
            Title = "Spesa",
            Items = ["Latte", "Pane"],
            DetectedLanguage = "it-IT",
            PromptVersion = "kinlist-audio-v2",
        }));
    }
}

internal sealed class FakeAudioOperationProcessor : IAudioOperationProcessor
{
    public TimeSpan ProcessDelay { get; set; }
    public Exception? ThrowOnProcess { get; set; }
    public ActivityTraceId? LastObservedTraceId { get; private set; }
    public Result<AudioProcessingOperationResponse> ProcessResult { get; set; } = Result<AudioProcessingOperationResponse>.Success(new AudioProcessingOperationResponse
    {
        Id = Guid.NewGuid(),
        Type = "NewList",
        Status = "Succeeded",
        RetryAfterSeconds = 2,
        ExpiresAt = DateTime.UtcNow.AddHours(1),
    });

    public List<(Guid OperationId, string Code, string Message)> MarkFailedCalls { get; } = [];

    public async Task<Result<AudioProcessingOperationResponse>> ProcessAudioOperationAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        LastObservedTraceId = Activity.Current?.TraceId;

        if (ThrowOnProcess is not null)
        {
            throw ThrowOnProcess;
        }

        if (ProcessDelay > TimeSpan.Zero)
        {
            await Task.Delay(ProcessDelay, cancellationToken);
        }

        return ProcessResult;
    }

    public Task<Result<AudioProcessingOperationResponse>> MarkAudioOperationFailedAsync(Guid operationId, string code, string message, CancellationToken cancellationToken = default)
    {
        MarkFailedCalls.Add((operationId, code, message));
        return Task.FromResult(Result<AudioProcessingOperationResponse>.Success(new AudioProcessingOperationResponse
        {
            Id = operationId,
            Type = "NewList",
            Status = "Failed",
            ErrorCode = code,
            ErrorMessage = message,
            RetryAfterSeconds = 2,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        }));
    }
}
