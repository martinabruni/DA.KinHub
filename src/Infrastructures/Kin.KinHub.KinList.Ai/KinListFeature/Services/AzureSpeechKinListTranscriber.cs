using Azure.AI.Speech.Transcription;
using Kin.KinHub.KinList.Ai.Common;
using System.ClientModel;

namespace Kin.KinHub.KinList.Ai.KinListFeature;

public sealed class AzureSpeechKinListTranscriber : IKinListSpeechTranscriber
{
    private readonly SpeechToTextOptions _options;
    private readonly KinListOptions _kinListOptions;

    public AzureSpeechKinListTranscriber(SpeechToTextOptions options, KinListOptions kinListOptions)
    {
        _options = options;
        _kinListOptions = kinListOptions;
    }

    public Task<Result<SpeechTranscriptionResult>> TranscribeAsync(KinListAudioCommand command, CancellationToken cancellationToken = default) =>
        ExecuteWithTimeoutAsync(
            ct => TranscribeCoreAsync(command, ct),
            cancellationToken);

    private async Task<Result<SpeechTranscriptionResult>> TranscribeCoreAsync(KinListAudioCommand command, CancellationToken cancellationToken)
    {
        var client = new TranscriptionClient(
            new Uri(_options.Endpoint),
            new ApiKeyCredential(_options.ApiKey),
            new TranscriptionClientOptions(TranscriptionClientOptions.ServiceVersion.V20251015));

        return await TransientExecutionHelper.ExecuteAsync(async ct =>
        {
            await using var stream = new MemoryStream(command.AudioBytes, writable: false);
            var transcriptionOptions = new TranscriptionOptions(stream);
            foreach (var locale in _options.CandidateLocales.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                transcriptionOptions.Locales.Add(locale);
            }

            var response = await client.TranscribeAsync(transcriptionOptions, ct);
            var transcript = string.Join(" ", response.Value.CombinedPhrases.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return Result<SpeechTranscriptionResult>.UnprocessableEntity(
                    "No actionable list items were detected in the audio.",
                    "no_items_detected");
            }

            var detectedLanguage = response.Value.Phrases
                .Select(x => x.Locale)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                ?? _options.CandidateLocales.FirstOrDefault()
                ?? "und";

            return Result<SpeechTranscriptionResult>.Success(new SpeechTranscriptionResult
            {
                Transcript = transcript,
                DetectedLanguage = detectedLanguage,
            });
        }, _kinListOptions.TransientRetryMaxAttempts, _kinListOptions.TransientRetryBaseDelayMilliseconds, _kinListOptions.TransientRetryMaxDelayMilliseconds, cancellationToken);
    }

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_kinListOptions.AudioProcessingTimeoutSeconds));

        try
        {
            return await operation(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (T)(object)Result<SpeechTranscriptionResult>.ServiceUnavailable(
                "Audio transcription timed out.",
                "audio_processing_timeout");
        }
    }
}
