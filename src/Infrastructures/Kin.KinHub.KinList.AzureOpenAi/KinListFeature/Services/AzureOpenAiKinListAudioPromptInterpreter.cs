using Kin.KinHub.KinList.AzureOpenAi.Common;
using System.Text.Json;

namespace Kin.KinHub.KinList.AzureOpenAi.KinListFeature;

public sealed class AzureOpenAiKinListAudioPromptInterpreter : IKinListAudioPromptInterpreter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IKinListChatCompletionClient _chatCompletionClient;
    private readonly KinListAudioPromptOptions _promptOptions;
    private readonly KinListOptions _kinListOptions;

    public AzureOpenAiKinListAudioPromptInterpreter(
        IKinListChatCompletionClient chatCompletionClient,
        KinListAudioPromptOptions promptOptions,
        KinListOptions kinListOptions)
    {
        _chatCompletionClient = chatCompletionClient;
        _promptOptions = promptOptions;
        _kinListOptions = kinListOptions;
    }

    public async Task<Result<ParsedKinListAudioDraft>> InterpretAsync(SpeechTranscriptionResult transcription, CancellationToken cancellationToken = default)
    {
        var request = JsonSerializer.Serialize(new
        {
            task = "kin_list_audio_draft",
            transcript = transcription.Transcript,
            detected_language = transcription.DetectedLanguage,
        }, JsonOptions);

        string responseJson;
        try
        {
            responseJson = await _chatCompletionClient.CompleteAsync(_promptOptions.SystemPrompt, request, cancellationToken);
        }
        catch (TimeoutException)
        {
            return Result<ParsedKinListAudioDraft>.ServiceUnavailable(
                "Audio structuring timed out.",
                "audio_processing_timeout");
        }
        catch (Exception)
        {
            return Result<ParsedKinListAudioDraft>.ServiceUnavailable(
                "Audio draft processing is currently unavailable.",
                "audio_processing_unavailable");
        }

        ParsedResponse? parsedResponse;
        try
        {
            parsedResponse = JsonSerializer.Deserialize<ParsedResponse>(responseJson, JsonOptions);
        }
        catch (JsonException)
        {
            return Result<ParsedKinListAudioDraft>.ServiceUnavailable(
                "Audio structuring returned an invalid response.",
                "audio_processing_invalid_response");
        }

        if (parsedResponse is null)
        {
            return Result<ParsedKinListAudioDraft>.ServiceUnavailable(
                "Audio structuring returned an empty response.",
                "audio_processing_invalid_response");
        }

        var normalizedItems = parsedResponse.Items
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var title = parsedResponse.Title?.Trim() ?? string.Empty;
        if (normalizedItems.Count > 0 && string.IsNullOrWhiteSpace(title))
        {
            return Result<ParsedKinListAudioDraft>.ServiceUnavailable(
                "Audio structuring returned an invalid title.",
                "audio_processing_invalid_response");
        }

        if (title.Length > _kinListOptions.MaxTitleLength)
        {
            return Result<ParsedKinListAudioDraft>.ServiceUnavailable(
                "Audio structuring returned a title outside the allowed limits.",
                "audio_processing_invalid_response");
        }

        if (normalizedItems.Count > _kinListOptions.MaxItemsPerBulkConfirm)
        {
            return Result<ParsedKinListAudioDraft>.ServiceUnavailable(
                "Audio structuring returned too many items.",
                "audio_processing_invalid_response");
        }

        if (normalizedItems.Any(x => x.Length > _kinListOptions.MaxItemLength))
        {
            return Result<ParsedKinListAudioDraft>.ServiceUnavailable(
                "Audio structuring returned an item outside the allowed limits.",
                "audio_processing_invalid_response");
        }

        return Result<ParsedKinListAudioDraft>.Success(new ParsedKinListAudioDraft
        {
            Title = title,
            Items = normalizedItems,
            DetectedLanguage = transcription.DetectedLanguage,
            PromptVersion = _promptOptions.PromptVersion,
        });
    }
}
