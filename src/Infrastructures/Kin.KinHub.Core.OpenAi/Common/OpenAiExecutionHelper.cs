using Azure;

namespace Kin.KinHub.Core.OpenAi.Common;

internal static class OpenAiExecutionHelper
{
    public static async Task<T> ExecuteWithResilienceAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string operationName,
        OpenAiOptions options,
        CancellationToken cancellationToken)
    {
        using var activity = OpenAiTelemetry.ActivitySource.StartActivity(operationName);
        activity?.SetTag("openai.retry.max_attempts", options.MaxRetryAttempts);
        activity?.SetTag("openai.timeout.seconds", options.RequestTimeoutSeconds);

        try
        {
            Exception? lastException = null;
            var attempts = Math.Max(1, options.MaxRetryAttempts);

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                activity?.SetTag("openai.retry.attempt", attempt);

                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
                    try
                    {
                        var result = await operation(timeoutCts.Token);
                        activity?.SetTag("openai.outcome", "success");
                        activity?.SetTag("openai.retry.final_attempt", attempt);
                        return result;
                    }
                    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
                    {
                        throw new TimeoutException("Azure OpenAI request timed out.", ex);
                    }
                }
                catch (Exception ex) when (attempt < attempts && IsTransient(ex))
                {
                    lastException = ex;
                    activity?.SetTag("openai.outcome", "retrying");
                    activity?.SetTag("openai.retry.final_attempt", attempt);
                    var backoff = Math.Min(options.MaxRetryDelayMilliseconds, options.BaseRetryDelayMilliseconds * (1 << (attempt - 1)));
                    var jitter = Random.Shared.Next(0, Math.Max(50, options.BaseRetryDelayMilliseconds));
                    await Task.Delay(backoff + jitter, cancellationToken);
                }
            }

            throw lastException ?? new InvalidOperationException("Azure OpenAI resilient execution failed without a captured exception.");
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            activity?.SetTag("openai.outcome", "unavailable");
            throw new RecipeAssistantUnavailableException("Azure OpenAI is temporarily unavailable.", ex);
        }
    }

    public static RecipeAssistantInvalidResponseException InvalidResponse(string message, string? payload = null, Exception? innerException = null) =>
        new(message, payload, innerException);

    private static bool IsTransient(Exception exception)
    {
        if (exception is TimeoutException or HttpRequestException or OperationCanceledException)
        {
            return true;
        }

        if (exception is RequestFailedException requestFailedException)
        {
            return requestFailedException.Status == 429 || requestFailedException.Status >= 500;
        }

        var statusProperty = exception.GetType().GetProperty("Status");
        if (statusProperty?.GetValue(exception) is int status)
        {
            return status == 429 || status >= 500;
        }

        return false;
    }
}
