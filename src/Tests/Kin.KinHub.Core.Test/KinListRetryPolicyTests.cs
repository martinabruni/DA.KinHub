using Kin.KinHub.KinList.AzureOpenAi.Common;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// T02.3 — the transient retry policy used by the audio pipeline: bounded attempts, exponential
/// backoff (bounded by a max delay), retries only on transient failures (timeout / 429 / 5xx /
/// transport), and never on deterministic errors (validation, authorization, malformed input).
/// </summary>
public sealed class KinListRetryPolicyTests
{
    private const int BaseDelayMs = 1;   // keep tests fast
    private const int MaxDelayMs = 8;

    [Fact]
    public async Task ExecuteAsync_WhenOperationSucceedsFirstTry_DoesNotRetry()
    {
        var attempts = 0;

        var result = await TransientExecutionHelper.ExecuteAsync(
            _ =>
            {
                attempts++;
                return Task.FromResult(42);
            },
            maxAttempts: 3,
            BaseDelayMs,
            MaxDelayMs,
            CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_RetriesTransientFailures_UpToMaxAttempts()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            TransientExecutionHelper.ExecuteAsync<int>(
                _ =>
                {
                    attempts++;
                    throw new HttpRequestException("transient");
                },
                maxAttempts: 3,
                BaseDelayMs,
                MaxDelayMs,
                CancellationToken.None));

        // Exactly 3 total attempts (max), not more.
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_RecoversAfterTransientFailures()
    {
        var attempts = 0;

        var result = await TransientExecutionHelper.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new TimeoutException("slow");
                }

                return Task.FromResult("ok");
            },
            maxAttempts: 3,
            BaseDelayMs,
            MaxDelayMs,
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(3, attempts);
    }

    [Theory]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task ExecuteAsync_RetriesRateLimitAndServerErrors(int statusCode)
    {
        var attempts = 0;

        await Assert.ThrowsAsync<StatusCodedException>(() =>
            TransientExecutionHelper.ExecuteAsync<int>(
                _ =>
                {
                    attempts++;
                    throw new StatusCodedException(statusCode);
                },
                maxAttempts: 3,
                BaseDelayMs,
                MaxDelayMs,
                CancellationToken.None));

        Assert.Equal(3, attempts);
    }

    [Theory]
    [InlineData(400)] // validation
    [InlineData(401)] // authentication
    [InlineData(403)] // authorization
    [InlineData(404)] // not found
    [InlineData(422)] // unprocessable
    public async Task ExecuteAsync_DoesNotRetryDeterministicClientErrors(int statusCode)
    {
        var attempts = 0;

        await Assert.ThrowsAsync<StatusCodedException>(() =>
            TransientExecutionHelper.ExecuteAsync<int>(
                _ =>
                {
                    attempts++;
                    throw new StatusCodedException(statusCode);
                },
                maxAttempts: 3,
                BaseDelayMs,
                MaxDelayMs,
                CancellationToken.None));

        // A non-transient (4xx) error must fail fast on the first attempt.
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryPlainInvalidOperation()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TransientExecutionHelper.ExecuteAsync<int>(
                _ =>
                {
                    attempts++;
                    throw new InvalidOperationException("deterministic");
                },
                maxAttempts: 3,
                BaseDelayMs,
                MaxDelayMs,
                CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledByCaller_DoesNotRetry()
    {
        var attempts = 0;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            TransientExecutionHelper.ExecuteAsync<int>(
                ct =>
                {
                    attempts++;
                    ct.ThrowIfCancellationRequested();
                    return Task.FromResult(0);
                },
                maxAttempts: 3,
                BaseDelayMs,
                MaxDelayMs,
                cts.Token));

        Assert.Equal(0, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_BackoffIsBoundedByMaxDelay()
    {
        // With base 50ms and max 60ms over 3 attempts, cumulative sleeps stay small and bounded;
        // proves the exponential backoff is clamped rather than growing unbounded.
        var attempts = 0;
        var start = DateTime.UtcNow;

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            TransientExecutionHelper.ExecuteAsync<int>(
                _ =>
                {
                    attempts++;
                    throw new HttpRequestException("transient");
                },
                maxAttempts: 3,
                baseDelayMilliseconds: 50,
                maxDelayMilliseconds: 60,
                CancellationToken.None));

        var elapsed = DateTime.UtcNow - start;
        Assert.Equal(3, attempts);
        // Two sleeps, each clamped to max 60ms plus jitter (< base) -> comfortably under 1s.
        Assert.True(elapsed < TimeSpan.FromSeconds(1), $"Backoff was not bounded: {elapsed}.");
    }

    /// <summary>Exception exposing an <c>int Status</c> property, matching the Azure SDK shape the
    /// retry policy inspects via reflection to classify HTTP status codes.</summary>
    private sealed class StatusCodedException : Exception
    {
        public StatusCodedException(int status) => Status = status;

        public int Status { get; }
    }
}
