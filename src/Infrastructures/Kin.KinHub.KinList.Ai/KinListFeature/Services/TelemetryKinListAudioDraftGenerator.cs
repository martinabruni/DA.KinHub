using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Kin.KinHub.KinList.Ai.KinListFeature;

/// <summary>
/// Decorates an <see cref="IKinListAudioDraftGenerator"/> to emit privacy-preserving telemetry for
/// each audio structuring attempt.
/// </summary>
/// <remarks>
/// Only non-sensitive, aggregate signals are logged: audio byte size, configured audio duration
/// budget, detected language code, end-to-end latency, outcome code, produced item count, prompt
/// version and a correlation id. The raw audio bytes, the transcript, the generated title and the
/// generated item texts are NEVER logged, so recordings and their contents cannot leak into logs.
/// </remarks>
public sealed class TelemetryKinListAudioDraftGenerator : IKinListAudioDraftGenerator
{
    private readonly IKinListAudioDraftGenerator _inner;
    private readonly ILogger<TelemetryKinListAudioDraftGenerator> _logger;

    public TelemetryKinListAudioDraftGenerator(
        IKinListAudioDraftGenerator inner,
        ILogger<TelemetryKinListAudioDraftGenerator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        Result<ParsedKinListAudioDraft> result;
        try
        {
            result = await _inner.ParseAsync(command, cancellationToken);
        }
        catch
        {
            stopwatch.Stop();
            _logger.LogError(
                "KinList audio draft failed. Bytes={AudioBytes} LatencyMs={LatencyMs} Outcome={Outcome} CorrelationId={CorrelationId}",
                command.AudioBytes.Length,
                stopwatch.ElapsedMilliseconds,
                "exception",
                correlationId);
            throw;
        }

        stopwatch.Stop();

        var outcome = result.IsSuccess ? "success" : result.Code ?? "failure";
        var itemCount = result.IsSuccess && result.Value is not null ? result.Value.Items.Count : 0;
        var detectedLanguage = result.IsSuccess && result.Value is not null ? result.Value.DetectedLanguage : "unknown";
        var promptVersion = result.IsSuccess && result.Value is not null ? result.Value.PromptVersion : "unknown";

        _logger.LogInformation(
            "KinList audio draft processed. Bytes={AudioBytes} LatencyMs={LatencyMs} DetectedLanguage={DetectedLanguage} Outcome={Outcome} ItemCount={ItemCount} PromptVersion={PromptVersion} CorrelationId={CorrelationId}",
            command.AudioBytes.Length,
            stopwatch.ElapsedMilliseconds,
            detectedLanguage,
            outcome,
            itemCount,
            promptVersion,
            correlationId);

        return result;
    }
}
