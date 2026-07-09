using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.KinList.Domain.KinListFeature;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Text.Json;

namespace Kin.KinHub.Core.Test;

public sealed class AudioProcessingQueueConsumerTests
{
    [Fact]
    public async Task ProcessAsync_WhenPayloadIsInvalid_MovesMessageToPoisonAndDeletes()
    {
        var queuePump = new FakeAudioProcessingQueuePump();
        var consumer = CreateConsumer(queuePump, new FakeAudioOperationProcessor());

        var disposition = await consumer.ProcessAsync("not-json", dequeueCount: 1, messageId: "m1", renewVisibilityAsync: null, CancellationToken.None);

        Assert.Equal(AudioQueueMessageDisposition.Delete, disposition);
        Assert.Single(queuePump.PoisonMessages);
        Assert.Equal("not-json", queuePump.PoisonMessages[0]);
    }

    [Fact]
    public async Task ProcessAsync_WhenContractVersionIsUnsupported_MovesMessageToPoisonAndDeletes()
    {
        var queuePump = new FakeAudioProcessingQueuePump();
        var consumer = CreateConsumer(queuePump, new FakeAudioOperationProcessor());
        var messageText = $$"""{"contractVersion":2,"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""";

        var disposition = await consumer.ProcessAsync(messageText, dequeueCount: 1, messageId: "m1b", renewVisibilityAsync: null, CancellationToken.None);

        Assert.Equal(AudioQueueMessageDisposition.Delete, disposition);
        Assert.Single(queuePump.PoisonMessages);
    }

    [Fact]
    public async Task ProcessAsync_WhenDequeueLimitExceeded_MarksFailedAndMovesToPoison()
    {
        var processor = new FakeAudioOperationProcessor();
        var queuePump = new FakeAudioProcessingQueuePump();
        var consumer = CreateConsumer(queuePump, processor, new KinListOptions { AudioProcessingMaxDequeues = 5 });
        var operationId = Guid.NewGuid();
        var messageText = $$"""{"operationId":"{{operationId}}","correlationId":"corr"}""";

        var disposition = await consumer.ProcessAsync(messageText, dequeueCount: 6, messageId: "m2", renewVisibilityAsync: null, CancellationToken.None);

        Assert.Equal(AudioQueueMessageDisposition.Delete, disposition);
        Assert.Single(processor.MarkFailedCalls);
        Assert.Equal(operationId, processor.MarkFailedCalls[0].OperationId);
        Assert.Single(queuePump.PoisonMessages);
    }

    [Fact]
    public async Task ProcessAsync_WhenProcessingSucceeds_ReturnsDelete()
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
        var consumer = CreateConsumer(queuePump, processor);
        var messageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""";

        var disposition = await consumer.ProcessAsync(messageText, dequeueCount: 1, messageId: "m3", renewVisibilityAsync: null, CancellationToken.None);

        Assert.Equal(AudioQueueMessageDisposition.Delete, disposition);
        Assert.Empty(queuePump.PoisonMessages);
    }

    [Fact]
    public async Task ProcessAsync_WhenProcessingIsTransient_ReturnsRetry()
    {
        var processor = new FakeAudioOperationProcessor
        {
            ProcessResult = Result<AudioProcessingOperationResponse>.ServiceUnavailable("temporarily unavailable", "audio_processing_unavailable"),
        };
        var queuePump = new FakeAudioProcessingQueuePump();
        var consumer = CreateConsumer(queuePump, processor);
        var messageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""";

        var disposition = await consumer.ProcessAsync(messageText, dequeueCount: 1, messageId: "m4", renewVisibilityAsync: null, CancellationToken.None);

        Assert.Equal(AudioQueueMessageDisposition.Retry, disposition);
        Assert.Empty(queuePump.PoisonMessages);
    }

    [Fact]
    public async Task ProcessAsync_WhenProcessingIsLong_RenewsVisibility()
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
        var consumer = CreateConsumer(queuePump, processor);
        var messageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""";
        var renewedCount = 0;

        await consumer.ProcessAsync(
            messageText,
            dequeueCount: 1,
            messageId: "m5",
            renewVisibilityAsync: _ =>
            {
                renewedCount += 1;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.True(renewedCount > 0);
    }

    [Fact]
    public async Task ProcessAsync_WhenProcessorThrows_ReturnsRetry()
    {
        var processor = new FakeAudioOperationProcessor
        {
            ThrowOnProcess = new InvalidOperationException("boom"),
        };
        var queuePump = new FakeAudioProcessingQueuePump();
        var consumer = CreateConsumer(queuePump, processor);
        var messageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"corr"}""";

        var disposition = await consumer.ProcessAsync(messageText, dequeueCount: 1, messageId: "m6", renewVisibilityAsync: null, CancellationToken.None);

        Assert.Equal(AudioQueueMessageDisposition.Retry, disposition);
        Assert.Empty(queuePump.PoisonMessages);
    }

    [Fact]
    public async Task ProcessAsync_WhenCorrelationIdContainsTraceContext_UsesItAsParent()
    {
        using var parent = new Activity("test-parent");
        parent.SetIdFormat(ActivityIdFormat.W3C);
        parent.Start();

        var processor = new FakeAudioOperationProcessor();
        var queuePump = new FakeAudioProcessingQueuePump();
        var consumer = CreateConsumer(queuePump, processor);
        var messageText = $$"""{"operationId":"{{Guid.NewGuid()}}","correlationId":"{{parent.Id}}"}""";

        await consumer.ProcessAsync(messageText, dequeueCount: 1, messageId: "m7", renewVisibilityAsync: null, CancellationToken.None);

        Assert.Equal(parent.TraceId, processor.LastObservedTraceId);
    }

    private static AudioProcessingQueueConsumer CreateConsumer(
        FakeAudioProcessingQueuePump queuePump,
        FakeAudioOperationProcessor processor,
        KinListOptions? options = null)
    {
        options ??= new KinListOptions();

        var services = new ServiceCollection();
        services.AddSingleton<IAudioOperationProcessor>(processor);
        var provider = services.BuildServiceProvider();

        return new AudioProcessingQueueConsumer(
            queuePump,
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<AudioProcessingQueueConsumer>.Instance);
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
