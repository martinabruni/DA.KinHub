using Kin.KinHub.KinList.AzureOpenAi.Common;
using Kin.KinHub.KinList.AzureOpenAi.KinListFeature;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.Core.Test;

public sealed class KinListAudioDraftGeneratorTests
{
    private static readonly KinListOptions DefaultKinListOptions = new();

    [Fact]
    public async Task InterpretAsync_WithValidJson_ReturnsPromptVersionAndDetectedLanguage()
    {
        var interpreter = new AzureOpenAiKinListAudioPromptInterpreter(
            new FakeChatCompletionClient("""{"title":"Spesa settimanale","items":["Latte","2 confezioni di latte"," Pane "]}"""),
            new KinListAudioPromptOptions { PromptVersion = "kinlist-audio-v2" },
            DefaultKinListOptions);

        var result = await interpreter.InterpretAsync(new SpeechTranscriptionResult
        {
            Transcript = "latte due confezioni di latte pane",
            DetectedLanguage = "it-IT",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Spesa settimanale", result.Value!.Title);
        Assert.Equal(["Latte", "2 confezioni di latte", "Pane"], result.Value.Items);
        Assert.Equal("it-IT", result.Value.DetectedLanguage);
        Assert.Equal("kinlist-audio-v2", result.Value.PromptVersion);
    }

    [Fact]
    public async Task InterpretAsync_WithInvalidJson_ReturnsServiceUnavailable()
    {
        var interpreter = new AzureOpenAiKinListAudioPromptInterpreter(
            new FakeChatCompletionClient("not-json"),
            new KinListAudioPromptOptions(),
            DefaultKinListOptions);

        var result = await interpreter.InterpretAsync(new SpeechTranscriptionResult
        {
            Transcript = "latte pane",
            DetectedLanguage = "it-IT",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ServiceUnavailable, result.Status);
        Assert.Equal("audio_processing_invalid_response", result.Code);
    }

    [Fact]
    public async Task ParseAsync_WhenTranscriptionFails_PropagatesUnprocessableEntity()
    {
        var generator = new AzureSpeechOpenAiKinListAudioDraftGenerator(
            new FakeSpeechTranscriber(Result<SpeechTranscriptionResult>.UnprocessableEntity("No actionable list items were detected in the audio.", "no_items_detected")),
            new FakePromptInterpreter());

        var result = await generator.ParseAsync(new KinListAudioCommand
        {
            AudioBytes = [1, 2, 3],
            ContentType = "audio/webm",
            FileName = "draft.webm",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.UnprocessableEntity, result.Status);
        Assert.Equal("no_items_detected", result.Code);
    }

    [Fact]
    public async Task ParseAsync_WithSuccessfulDependencies_ReturnsDraft()
    {
        var generator = new AzureSpeechOpenAiKinListAudioDraftGenerator(
            new FakeSpeechTranscriber(Result<SpeechTranscriptionResult>.Success(new SpeechTranscriptionResult
            {
                Transcript = "latte pane",
                DetectedLanguage = "it-IT",
            })),
            new FakePromptInterpreter(Result<ParsedKinListAudioDraft>.Success(new ParsedKinListAudioDraft
            {
                Title = "Spesa",
                Items = ["Latte", "Pane"],
                DetectedLanguage = "it-IT",
                PromptVersion = "kinlist-audio-v1",
            })));

        var result = await generator.ParseAsync(new KinListAudioCommand
        {
            AudioBytes = [1, 2, 3],
            ContentType = "audio/webm",
            FileName = "draft.webm",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Spesa", result.Value!.Title);
        Assert.Equal(["Latte", "Pane"], result.Value.Items);
    }
}

internal sealed class FakeSpeechTranscriber : IKinListSpeechTranscriber
{
    private readonly Result<SpeechTranscriptionResult> _result;

    public FakeSpeechTranscriber(Result<SpeechTranscriptionResult> result)
    {
        _result = result;
    }

    public Task<Result<SpeechTranscriptionResult>> TranscribeAsync(KinListAudioCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);
}

internal sealed class FakePromptInterpreter : IKinListAudioPromptInterpreter
{
    private readonly Result<ParsedKinListAudioDraft> _result;

    public FakePromptInterpreter()
        : this(Result<ParsedKinListAudioDraft>.Success(new ParsedKinListAudioDraft
        {
            Title = "Spesa",
            Items = ["Latte"],
            DetectedLanguage = "it-IT",
            PromptVersion = "kinlist-audio-v1",
        }))
    {
    }

    public FakePromptInterpreter(Result<ParsedKinListAudioDraft> result)
    {
        _result = result;
    }

    public Task<Result<ParsedKinListAudioDraft>> InterpretAsync(SpeechTranscriptionResult transcription, CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);
}

internal sealed class FakeChatCompletionClient : IKinListChatCompletionClient
{
    private readonly string _response;

    public FakeChatCompletionClient(string response)
    {
        _response = response;
    }

    public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default) =>
        Task.FromResult(_response);
}
