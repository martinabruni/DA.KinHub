using Kin.KinHub.KinList.Ai.Common;
using Kin.KinHub.KinList.Ai.KinListFeature;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// T03 — audio pipeline behavior with fully deterministic Speech/OpenAI fakes. No test in this
/// file performs any real Azure call; every dependency is a local fake.
/// </summary>
public sealed class KinListAudioPipelineTests
{
    private static readonly KinListOptions DefaultKinListOptions = new();

    private static KinListAudioCommand Audio() => new()
    {
        AudioBytes = [1, 2, 3, 4, 5],
        ContentType = "audio/webm",
        FileName = "draft.webm",
    };

    // ---------- T03.1 deterministic pipeline: language detection, invalid output, transient ----------

    [Fact]
    public async Task Pipeline_PreservesAutoDetectedLanguageFromTranscriber()
    {
        var generator = new AzureSpeechOpenAiKinListAudioDraftGenerator(
            new FakeSpeechTranscriber(Result<SpeechTranscriptionResult>.Success(new SpeechTranscriptionResult
            {
                Transcript = "milk bread",
                DetectedLanguage = "en-US",
            })),
            new AzureOpenAiKinListAudioPromptInterpreter(
                new FakeChatCompletionClient("""{"title":"Groceries","items":["Milk","Bread"]}"""),
                new KinListAudioPromptOptions(),
                DefaultKinListOptions));

        var result = await generator.ParseAsync(Audio());

        Assert.True(result.IsSuccess);
        // The interpreter must echo the transcriber's detected language, not guess its own.
        Assert.Equal("en-US", result.Value!.DetectedLanguage);
    }

    [Fact]
    public async Task Pipeline_WhenStructuredOutputInvalid_ReturnsServiceUnavailable()
    {
        var generator = new AzureSpeechOpenAiKinListAudioDraftGenerator(
            new FakeSpeechTranscriber(Result<SpeechTranscriptionResult>.Success(new SpeechTranscriptionResult
            {
                Transcript = "latte pane",
                DetectedLanguage = "it-IT",
            })),
            new AzureOpenAiKinListAudioPromptInterpreter(
                new FakeChatCompletionClient("this is not json"),
                new KinListAudioPromptOptions(),
                DefaultKinListOptions));

        var result = await generator.ParseAsync(Audio());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ServiceUnavailable, result.Status);
        Assert.Equal("audio_processing_invalid_response", result.Code);
    }

    [Fact]
    public async Task Pipeline_WhenChatClientTimesOut_ReturnsTimeoutServiceUnavailable()
    {
        var generator = new AzureSpeechOpenAiKinListAudioDraftGenerator(
            new FakeSpeechTranscriber(Result<SpeechTranscriptionResult>.Success(new SpeechTranscriptionResult
            {
                Transcript = "latte pane",
                DetectedLanguage = "it-IT",
            })),
            new AzureOpenAiKinListAudioPromptInterpreter(
                new ThrowingChatCompletionClient(new TimeoutException("slow")),
                new KinListAudioPromptOptions(),
                DefaultKinListOptions));

        var result = await generator.ParseAsync(Audio());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ServiceUnavailable, result.Status);
        Assert.Equal("audio_processing_timeout", result.Code);
    }

    [Fact]
    public async Task Pipeline_WhenChatClientThrowsTransient_ReturnsServiceUnavailable()
    {
        var generator = new AzureSpeechOpenAiKinListAudioDraftGenerator(
            new FakeSpeechTranscriber(Result<SpeechTranscriptionResult>.Success(new SpeechTranscriptionResult
            {
                Transcript = "latte pane",
                DetectedLanguage = "it-IT",
            })),
            new AzureOpenAiKinListAudioPromptInterpreter(
                new ThrowingChatCompletionClient(new HttpRequestException("429")),
                new KinListAudioPromptOptions(),
                DefaultKinListOptions));

        var result = await generator.ParseAsync(Audio());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ServiceUnavailable, result.Status);
        Assert.Equal("audio_processing_unavailable", result.Code);
    }

    [Fact]
    public async Task Pipeline_WhenTranscriberFindsNothing_PropagatesNoItemsDetected()
    {
        var generator = new AzureSpeechOpenAiKinListAudioDraftGenerator(
            new FakeSpeechTranscriber(Result<SpeechTranscriptionResult>.UnprocessableEntity(
                "No actionable list items were detected in the audio.", "no_items_detected")),
            new FakePromptInterpreter());

        var result = await generator.ParseAsync(Audio());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.UnprocessableEntity, result.Status);
        Assert.Equal("no_items_detected", result.Code);
    }

    // ---------- T03.2 versioned prompt semantics ----------

    [Fact]
    public async Task Interpreter_StampsConfiguredPromptVersion()
    {
        var interpreter = new AzureOpenAiKinListAudioPromptInterpreter(
            new FakeChatCompletionClient("""{"title":"Spesa","items":["Latte"]}"""),
            new KinListAudioPromptOptions { PromptVersion = "kinlist-audio-v3" },
            DefaultKinListOptions);

        var result = await interpreter.InterpretAsync(new SpeechTranscriptionResult
        {
            Transcript = "latte",
            DetectedLanguage = "it-IT",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("kinlist-audio-v3", result.Value!.PromptVersion);
    }

    [Fact]
    public async Task Interpreter_PreservesLanguageAndKeepsQuantityUnitConcatenated()
    {
        var interpreter = new AzureOpenAiKinListAudioPromptInterpreter(
            new FakeChatCompletionClient("""{"title":"Spesa","items":["2 confezioni di latte","Pane"]}"""),
            new KinListAudioPromptOptions(),
            DefaultKinListOptions);

        var result = await interpreter.InterpretAsync(new SpeechTranscriptionResult
        {
            Transcript = "due confezioni di latte pane",
            DetectedLanguage = "it-IT",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("it-IT", result.Value!.DetectedLanguage);
        // Quantity + unit stay in a single item line.
        Assert.Contains("2 confezioni di latte", result.Value.Items);
    }

    [Fact]
    public async Task Interpreter_DeduplicatesOnlyAfterWhitespaceNormalization()
    {
        var interpreter = new AzureOpenAiKinListAudioPromptInterpreter(
            new FakeChatCompletionClient("""{"title":"Spesa","items":["Latte"," Latte ","latte"]}"""),
            new KinListAudioPromptOptions(),
            DefaultKinListOptions);

        var result = await interpreter.InterpretAsync(new SpeechTranscriptionResult
        {
            Transcript = "latte latte latte",
            DetectedLanguage = "it-IT",
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items); // all three normalize to the same item
        Assert.Equal("Latte", result.Value.Items[0]);
    }

    [Fact]
    public async Task Interpreter_KeepsDifferentQuantitiesAsSeparateItems()
    {
        var interpreter = new AzureOpenAiKinListAudioPromptInterpreter(
            new FakeChatCompletionClient("""{"title":"Spesa","items":["1 litro di latte","2 litri di latte"]}"""),
            new KinListAudioPromptOptions(),
            DefaultKinListOptions);

        var result = await interpreter.InterpretAsync(new SpeechTranscriptionResult
        {
            Transcript = "un litro di latte due litri di latte",
            DetectedLanguage = "it-IT",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
    }

    [Fact]
    public async Task Interpreter_WhenNoItems_ReturnsEmptyDraftRegardlessOfTitle()
    {
        var interpreter = new AzureOpenAiKinListAudioPromptInterpreter(
            new FakeChatCompletionClient("""{"title":"","items":[]}"""),
            new KinListAudioPromptOptions(),
            DefaultKinListOptions);

        var result = await interpreter.InterpretAsync(new SpeechTranscriptionResult
        {
            Transcript = "hello there",
            DetectedLanguage = "en-US",
        });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task Interpreter_WhenTitleExceedsLimit_ReturnsInvalidResponse()
    {
        var interpreter = new AzureOpenAiKinListAudioPromptInterpreter(
            new FakeChatCompletionClient($$"""{"title":"{{new string('x', 101)}}","items":["Latte"]}"""),
            new KinListAudioPromptOptions(),
            DefaultKinListOptions);

        var result = await interpreter.InterpretAsync(new SpeechTranscriptionResult
        {
            Transcript = "latte",
            DetectedLanguage = "it-IT",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ServiceUnavailable, result.Status);
        Assert.Equal("audio_processing_invalid_response", result.Code);
    }

    [Fact]
    public async Task Interpreter_WhenItemsExceedLimit_ReturnsInvalidResponse()
    {
        var items = string.Join(',', Enumerable.Range(0, 51).Select(x => $@"""item-{x}"""));
        var interpreter = new AzureOpenAiKinListAudioPromptInterpreter(
            new FakeChatCompletionClient($$"""{"title":"Spesa","items":[{{items}}]}"""),
            new KinListAudioPromptOptions(),
            DefaultKinListOptions);

        var result = await interpreter.InterpretAsync(new SpeechTranscriptionResult
        {
            Transcript = "spesa",
            DetectedLanguage = "it-IT",
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.ServiceUnavailable, result.Status);
        Assert.Equal("audio_processing_invalid_response", result.Code);
    }

    // ---------- T03.3 service-level draft responses ----------

    [Fact]
    public async Task CreateDraftFromAudio_ReturnsTitleItemsLanguageAndPromptVersion()
    {
        var service = BuildService(out _, new ParsedKinListAudioDraft
        {
            Title = "Spesa settimanale",
            Items = ["Latte", "Pane", "Uova"],
            DetectedLanguage = "it-IT",
            PromptVersion = "kinlist-audio-v2",
        });

        var result = await service.CreateDraftFromAudioAsync(Audio());

        Assert.True(result.IsSuccess);
        Assert.Equal("Spesa settimanale", result.Value!.Title);
        Assert.Equal(3, result.Value.Items.Count);
        Assert.Equal("it-IT", result.Value.DetectedLanguage);
        Assert.Equal("kinlist-audio-v2", result.Value.PromptVersion);
    }

    [Fact]
    public async Task CreateDraftFromAudio_WhenNoItems_Returns422NoItemsDetected()
    {
        var service = BuildService(out _, new ParsedKinListAudioDraft
        {
            Title = "Spesa",
            Items = [],
            DetectedLanguage = "it-IT",
            PromptVersion = "kinlist-audio-v1",
        });

        var result = await service.CreateDraftFromAudioAsync(Audio());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.UnprocessableEntity, result.Status);
        Assert.Equal("no_items_detected", result.Code);
    }

    [Fact]
    public async Task CreateItemDraftsFromAudio_MarksExistingDuplicatesDeselectedAndNewItemsSelected()
    {
        var service = BuildService(out var store, new ParsedKinListAudioDraft
        {
            Title = "Spesa",
            Items = ["Latte", "Pane", "Uova"],
            DetectedLanguage = "it-IT",
            PromptVersion = "kinlist-audio-v1",
        });
        var familyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var created = await service.CreateAsync(new CreateKinListRequest { Title = "Spesa", Items = ["Latte"] }, familyId, userId, "req-1");

        var drafts = await service.CreateItemDraftsFromAudioAsync(created.Value!.Id, familyId, Audio());

        Assert.True(drafts.IsSuccess);
        Assert.Equal(3, drafts.Value!.Items.Count);
        // "Latte" already exists -> proposed but deselected by default, and surfaced as a duplicate.
        var latte = drafts.Value.Items.Single(x => x.Text == "Latte");
        Assert.False(latte.IsSelectedByDefault);
        Assert.NotNull(latte.DuplicateOfItemId);
        // New items are selected by default.
        Assert.True(drafts.Value.Items.Single(x => x.Text == "Pane").IsSelectedByDefault);
        Assert.Single(drafts.Value.ExistingDuplicates);
        _ = store;
    }

    [Fact]
    public async Task CreateItemDraftsFromAudio_WhenListBelongsToAnotherFamily_ReturnsUnauthorized()
    {
        var service = BuildService(out _, new ParsedKinListAudioDraft
        {
            Title = "Spesa",
            Items = ["Latte"],
            DetectedLanguage = "it-IT",
            PromptVersion = "kinlist-audio-v1",
        });
        var familyA = Guid.NewGuid();
        var familyB = Guid.NewGuid();

        var created = await service.CreateAsync(new CreateKinListRequest { Title = "Spesa", Items = ["Latte"] }, familyA, Guid.NewGuid(), "req-1");

        var drafts = await service.CreateItemDraftsFromAudioAsync(created.Value!.Id, familyB, Audio());

        Assert.False(drafts.IsSuccess);
        Assert.Equal(ResultStatus.Unauthorized, drafts.Status);
    }

    // ---------- T03.4 telemetry: only safe fields, never audio/transcript/title/items ----------

    [Fact]
    public async Task Telemetry_LogsOnlyAllowedFields_AndNeverContentOnSuccess()
    {
        var logger = new CapturingLogger<TelemetryKinListAudioDraftGenerator>();
        var inner = new StubAudioDraftGenerator(Result<ParsedKinListAudioDraft>.Success(new ParsedKinListAudioDraft
        {
            Title = "Spesa segreta della famiglia",
            Items = ["Latte intero", "Pane fresco", "Documenti riservati"],
            DetectedLanguage = "it-IT",
            PromptVersion = "kinlist-audio-v2",
        }));
        var telemetry = new TelemetryKinListAudioDraftGenerator(inner, logger);

        var result = await telemetry.ParseAsync(Audio());

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(logger.Entries);

        // Allowed, non-sensitive fields must be present.
        Assert.Contains("Bytes", entry.Message);
        Assert.Contains("LatencyMs", entry.Message);
        Assert.Contains("it-IT", entry.Message);       // detected language
        Assert.Contains("success", entry.Message);      // outcome
        Assert.Contains("kinlist-audio-v2", entry.Message); // prompt version
        Assert.Contains("CorrelationId", entry.Message);
        Assert.Contains("ItemCount", entry.Message);

        // Sensitive content must NEVER appear anywhere in the captured log output.
        var everything = entry.Message + " " + string.Join(" ", entry.StateValues.Select(x => x.Value?.ToString()));
        Assert.DoesNotContain("Spesa segreta", everything);
        Assert.DoesNotContain("Latte intero", everything);
        Assert.DoesNotContain("Pane fresco", everything);
        Assert.DoesNotContain("Documenti riservati", everything);
    }

    [Fact]
    public async Task Telemetry_OnFailure_LogsOutcomeCodeWithoutContent()
    {
        var logger = new CapturingLogger<TelemetryKinListAudioDraftGenerator>();
        var inner = new StubAudioDraftGenerator(Result<ParsedKinListAudioDraft>.ServiceUnavailable(
            "Audio structuring returned an invalid response.", "audio_processing_invalid_response"));
        var telemetry = new TelemetryKinListAudioDraftGenerator(inner, logger);

        var result = await telemetry.ParseAsync(Audio());

        Assert.False(result.IsSuccess);
        var entry = Assert.Single(logger.Entries);
        Assert.Contains("audio_processing_invalid_response", entry.Message); // outcome code is safe metadata
        Assert.Contains("ItemCount", entry.Message);
    }

    [Fact]
    public async Task Telemetry_DoesNotEmitRawAudioBytesValue()
    {
        var logger = new CapturingLogger<TelemetryKinListAudioDraftGenerator>();
        var inner = new StubAudioDraftGenerator(Result<ParsedKinListAudioDraft>.Success(new ParsedKinListAudioDraft
        {
            Title = "T",
            Items = ["A"],
            DetectedLanguage = "it-IT",
            PromptVersion = "v1",
        }));
        var telemetry = new TelemetryKinListAudioDraftGenerator(inner, logger);
        var command = Audio();

        await telemetry.ParseAsync(command);

        var entry = Assert.Single(logger.Entries);
        // Only the byte COUNT is logged (a number), never a byte array dump.
        var bytesValue = entry.StateValues.Single(x => x.Key == "AudioBytes").Value;
        Assert.Equal(command.AudioBytes.Length, Assert.IsType<int>(bytesValue));
    }

    // ---------- helpers ----------

    private static KinListService BuildService(out InMemoryKinListStore store, ParsedKinListAudioDraft draft)
    {
        store = new InMemoryKinListStore();
        return new KinListService(
            store,
            store,
            store,
            store,
            new TestKinListTransactionExecutor(),
            new StubAudioDraftGenerator(Result<ParsedKinListAudioDraft>.Success(draft)),
            new InMemoryAudioBlobStorage(),
            new InMemoryAudioProcessingQueue(),
            new KinListOptions());
    }
}

internal sealed class ThrowingChatCompletionClient : IKinListChatCompletionClient
{
    private readonly Exception _exception;

    public ThrowingChatCompletionClient(Exception exception) => _exception = exception;

    public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default) =>
        throw _exception;
}

internal sealed class StubAudioDraftGenerator : IKinListAudioDraftGenerator
{
    private readonly Result<ParsedKinListAudioDraft> _result;

    public StubAudioDraftGenerator(Result<ParsedKinListAudioDraft> result) => _result = result;

    public Task<Result<ParsedKinListAudioDraft>> ParseAsync(KinListAudioCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);
}
