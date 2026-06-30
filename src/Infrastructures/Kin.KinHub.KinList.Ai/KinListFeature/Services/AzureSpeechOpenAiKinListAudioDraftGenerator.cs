namespace Kin.KinHub.KinList.Ai.KinListFeature;

public sealed class AzureSpeechOpenAiKinListAudioDraftGenerator : IKinListAudioDraftGenerator
{
    private readonly IKinListSpeechTranscriber _speechTranscriber;
    private readonly IKinListAudioPromptInterpreter _promptInterpreter;

    public AzureSpeechOpenAiKinListAudioDraftGenerator(
        IKinListSpeechTranscriber speechTranscriber,
        IKinListAudioPromptInterpreter promptInterpreter)
    {
        _speechTranscriber = speechTranscriber;
        _promptInterpreter = promptInterpreter;
    }

    public async Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default)
    {
        var transcription = await _speechTranscriber.TranscribeAsync(command, cancellationToken);
        if (!transcription.IsSuccess || transcription.Value is null)
        {
            return transcription.Status switch
            {
                ResultStatus.UnprocessableEntity => Result<ParsedKinListAudioDraft>.UnprocessableEntity(transcription.Message ?? "No actionable list items were detected in the audio.", transcription.Code ?? "no_items_detected"),
                ResultStatus.ValidationError => Result<ParsedKinListAudioDraft>.ValidationError(transcription.Message ?? "Audio request is invalid.", transcription.Code ?? "validation_error"),
                _ => Result<ParsedKinListAudioDraft>.ServiceUnavailable(transcription.Message ?? "Audio transcription is unavailable.", transcription.Code ?? "audio_processing_unavailable"),
            };
        }

        return await _promptInterpreter.InterpretAsync(transcription.Value, cancellationToken);
    }
}
