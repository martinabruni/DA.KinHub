namespace Kin.KinHub.KinList.Ai.Common;

internal static class TransientExecutionHelper
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxAttempts,
        int baseDelayMilliseconds,
        int maxDelayMilliseconds,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, maxAttempts);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (attempt < attempts && IsTransient(ex, cancellationToken))
            {
                lastException = ex;
                var backoff = Math.Min(maxDelayMilliseconds, baseDelayMilliseconds * (1 << (attempt - 1)));
                var jitter = Random.Shared.Next(0, Math.Max(50, baseDelayMilliseconds));
                await Task.Delay(backoff + jitter, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Transient execution failed without a captured exception.");
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return true;
        }

        if (exception is TimeoutException or HttpRequestException)
        {
            return true;
        }

        if (TryGetStatusCode(exception, out var statusCode))
        {
            return statusCode == 429 || statusCode >= 500;
        }

        return false;
    }

    private static bool TryGetStatusCode(Exception exception, out int statusCode)
    {
        var property = exception.GetType().GetProperty("Status");
        if (property?.GetValue(exception) is int status)
        {
            statusCode = status;
            return true;
        }

        var nullableStatus = property?.GetValue(exception) as int?;
        if (nullableStatus.HasValue)
        {
            statusCode = nullableStatus.Value;
            return true;
        }

        statusCode = 0;
        return false;
    }
}
