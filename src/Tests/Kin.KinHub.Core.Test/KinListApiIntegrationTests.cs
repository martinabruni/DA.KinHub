using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinList.Business.KinListFeature;

namespace Kin.KinHub.Core.Test;

/// <summary>
/// Full HTTP integration tests for the KinList API. A single host is shared across the class via
/// <see cref="IClassFixture{TFixture}"/> (creating a WebApplicationFactory per test exhausts host
/// resources). State is reset at the start of each test through <see cref="ResetState"/>.
/// </summary>
public sealed class KinListApiIntegrationTests : IClassFixture<KinListApiFactory>
{
    private readonly KinListApiFactory _factory;

    public KinListApiIntegrationTests(KinListApiFactory factory)
    {
        _factory = factory;
        ResetState();
    }

    private void ResetState()
    {
        _factory.CurrentUser.IsAuthenticated = true;
        _factory.CurrentUser.HasFamilyContext = true;
        _factory.CurrentUser.FamilyId = KinListApiFactory.FamilyA;
        _factory.CurrentUser.UserId = KinListApiFactory.UserId;
        _factory.AudioGenerator.Reset();
        _factory.BlobStorage.Reset();
        _factory.AudioQueue.Reset();
    }

    // ---------- CRUD happy path ----------

    [Fact]
    public async Task GetAll_WhenListsExist_ReturnsFamilyLists()
    {
        using var client = _factory.CreateClient();
        await CreateListAsync(client, "Spesa", ["Latte", "Pane"], key: Guid.NewGuid().ToString());

        var response = await client.GetAsync("/api/lists");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithEtagAndItems()
    {
        using var client = _factory.CreateClient();

        var response = await CreateListAsync(client, "Spesa", ["Latte", "Pane"], key: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Spesa", detail.GetProperty("title").GetString());
        Assert.Equal(2, detail.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetById_ReturnsListWithEtagHeader()
    {
        using var client = _factory.CreateClient();
        var created = await CreateListAsync(client, "Spesa", ["Latte"], key: Guid.NewGuid().ToString());
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await client.GetAsync($"/api/lists/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/lists/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidEtag_UpdatesTitleAndBumpsVersion()
    {
        using var client = _factory.CreateClient();
        var created = await CreateListAsync(client, "Spesa", ["Latte"], key: Guid.NewGuid().ToString());
        var (id, etag) = await ReadIdAndEtagAsync(created);

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/lists/{id}")
        {
            Content = JsonContent.Create(new { title = "Spesa aggiornata" }),
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Spesa aggiornata", detail.GetProperty("title").GetString());
        Assert.NotEqual(etag, detail.GetProperty("eTag").GetString());
    }

    // ---------- Items ----------

    [Fact]
    public async Task AddItem_ThenBulkConfirm_ThenUpdateAndDeleteItem_Flow()
    {
        using var client = _factory.CreateClient();
        var created = await CreateListAsync(client, "Spesa", ["Latte"], key: Guid.NewGuid().ToString());
        var (id, etag) = await ReadIdAndEtagAsync(created);

        // add single item
        var afterAdd = await PostWithIfMatchAsync(client, $"/api/lists/{id}/items", new { text = "Pane" }, etag);
        Assert.Equal(HttpStatusCode.OK, afterAdd.StatusCode);
        var addBody = await afterAdd.Content.ReadFromJsonAsync<JsonElement>();
        etag = addBody.GetProperty("eTag").GetString()!;
        Assert.Equal(2, addBody.GetProperty("items").GetArrayLength());

        // bulk confirm
        var afterBulk = await PostWithIfMatchAsync(client, $"/api/lists/{id}/items/confirm", new { items = new[] { "Uova", "Burro" } }, etag);
        Assert.Equal(HttpStatusCode.OK, afterBulk.StatusCode);
        var bulkBody = await afterBulk.Content.ReadFromJsonAsync<JsonElement>();
        etag = bulkBody.GetProperty("eTag").GetString()!;
        Assert.Equal(4, bulkBody.GetProperty("items").GetArrayLength());

        // pick an item to update
        var item = bulkBody.GetProperty("items").EnumerateArray().First(x => x.GetProperty("text").GetString() == "Uova");
        var itemId = item.GetProperty("id").GetGuid();
        var itemEtag = item.GetProperty("eTag").GetString()!;

        using var updateItem = new HttpRequestMessage(HttpMethod.Patch, $"/api/lists/{id}/items/{itemId}")
        {
            Content = JsonContent.Create(new { text = "Uova fresche", isCompleted = true }),
        };
        updateItem.Headers.TryAddWithoutValidation("If-Match", itemEtag);
        var updateItemResponse = await client.SendAsync(updateItem);
        Assert.Equal(HttpStatusCode.OK, updateItemResponse.StatusCode);
        var updatedBody = await updateItemResponse.Content.ReadFromJsonAsync<JsonElement>();
        var updatedItem = updatedBody.GetProperty("items").EnumerateArray().First(x => x.GetProperty("id").GetGuid() == itemId);
        Assert.Equal("Uova fresche", updatedItem.GetProperty("text").GetString());
        Assert.True(updatedItem.GetProperty("isCompleted").GetBoolean());

        // delete item then restore it
        var freshItemEtag = updatedItem.GetProperty("eTag").GetString()!;
        using var deleteItem = new HttpRequestMessage(HttpMethod.Delete, $"/api/lists/{id}/items/{itemId}");
        deleteItem.Headers.TryAddWithoutValidation("If-Match", freshItemEtag);
        var deleteResponse = await client.SendAsync(deleteItem);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var afterDelete = await deleteResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(afterDelete.GetProperty("items").EnumerateArray(), x => x.GetProperty("id").GetGuid() == itemId);
    }

    [Fact]
    public async Task DifferentItems_AreUpdatableIndependently()
    {
        using var client = _factory.CreateClient();
        var created = await CreateListAsync(client, "Spesa", ["Latte", "Pane"], key: Guid.NewGuid().ToString());
        var detail = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = detail.GetProperty("id").GetGuid();
        var items = detail.GetProperty("items").EnumerateArray().ToList();
        var first = items[0];
        var second = items[1];

        // Update the first item using ITS etag.
        var updateFirst = await PatchItemAsync(client, id, first.GetProperty("id").GetGuid(), first.GetProperty("eTag").GetString()!, "Latte scremato", false);
        Assert.Equal(HttpStatusCode.OK, updateFirst.StatusCode);

        // The second item's original etag is still valid because item versions are independent.
        var updateSecond = await PatchItemAsync(client, id, second.GetProperty("id").GetGuid(), second.GetProperty("eTag").GetString()!, "Pane integrale", false);
        Assert.Equal(HttpStatusCode.OK, updateSecond.StatusCode);
    }

    // ---------- ETag enforcement ----------

    [Fact]
    public async Task Update_WithoutIfMatch_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var created = await CreateListAsync(client, "Spesa", ["Latte"], key: Guid.NewGuid().ToString());
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var response = await client.PatchAsJsonAsync($"/api/lists/{id}", new { title = "Nuovo" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("if_match_required", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Update_WithStaleEtag_ReturnsConflictWithEtagConflictCode()
    {
        using var client = _factory.CreateClient();
        var created = await CreateListAsync(client, "Spesa", ["Latte"], key: Guid.NewGuid().ToString());
        var (id, etag) = await ReadIdAndEtagAsync(created);

        // First update consumes the etag and rotates the version.
        var first = await PatchListAsync(client, id, etag, "Prima modifica");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Reusing the now-stale etag must conflict and must NOT be retried server-side.
        var second = await PatchListAsync(client, id, etag, "Seconda modifica");
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var body = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("etag_conflict", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ItemMutation_BumpsListVersionAndTimestamp()
    {
        using var client = _factory.CreateClient();
        var created = await CreateListAsync(client, "Spesa", ["Latte"], key: Guid.NewGuid().ToString());
        var beforeDetail = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = beforeDetail.GetProperty("id").GetGuid();
        var listEtag = beforeDetail.GetProperty("eTag").GetString()!;
        var beforeModified = beforeDetail.GetProperty("lastModifiedAt").GetDateTime();

        await Task.Delay(5);
        var afterAdd = await PostWithIfMatchAsync(client, $"/api/lists/{id}/items", new { text = "Pane" }, listEtag);
        Assert.Equal(HttpStatusCode.OK, afterAdd.StatusCode);
        var afterDetail = await afterAdd.Content.ReadFromJsonAsync<JsonElement>();

        Assert.NotEqual(listEtag, afterDetail.GetProperty("eTag").GetString());
        Assert.True(afterDetail.GetProperty("lastModifiedAt").GetDateTime() >= beforeModified);
    }

    // ---------- Family isolation ----------

    [Fact]
    public async Task FamilyIsolation_FamilyBCannotAccessFamilyAList()
    {
        using var client = _factory.CreateClient();

        _factory.CurrentUser.FamilyId = KinListApiFactory.FamilyA;
        var created = await CreateListAsync(client, "Spesa A", ["Latte"], key: Guid.NewGuid().ToString());
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Switch the caller to family B and attempt to read family A's list.
        _factory.CurrentUser.FamilyId = KinListApiFactory.FamilyB;
        var response = await client.GetAsync($"/api/lists/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("forbidden", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task FamilyIsolation_GetAllOnlyReturnsCallerFamilyLists()
    {
        using var client = _factory.CreateClient();

        _factory.CurrentUser.FamilyId = KinListApiFactory.FamilyA;
        await CreateListAsync(client, "Solo A", ["Latte"], key: Guid.NewGuid().ToString());

        _factory.CurrentUser.FamilyId = KinListApiFactory.FamilyB;
        var response = await client.GetAsync("/api/lists");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.DoesNotContain(body.EnumerateArray(), x => x.GetProperty("title").GetString() == "Solo A");
    }

    // ---------- Soft delete + restore ----------

    [Fact]
    public async Task SoftDelete_HidesListFromGet_AndRestoreBringsItBack()
    {
        using var client = _factory.CreateClient();
        var created = await CreateListAsync(client, "Spesa", ["Latte"], key: Guid.NewGuid().ToString());
        var (id, etag) = await ReadIdAndEtagAsync(created);

        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/lists/{id}");
        delete.Headers.TryAddWithoutValidation("If-Match", etag);
        var deleteResponse = await client.SendAsync(delete);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var deletedEtag = (await deleteResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eTag").GetString()!;

        // Deleted list is not retrievable.
        var getDeleted = await client.GetAsync($"/api/lists/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);

        // Restore requires the current (post-delete) etag.
        var restore = await PostWithIfMatchAsync(client, $"/api/lists/{id}/restore", new { }, deletedEtag);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);

        var getRestored = await client.GetAsync($"/api/lists/{id}");
        Assert.Equal(HttpStatusCode.OK, getRestored.StatusCode);
    }

    // ---------- Authorization / family context guards ----------

    [Fact]
    public async Task WhenNoFamilyContext_ReturnsForbiddenFamilyRequired()
    {
        using var client = _factory.CreateClient();
        _factory.CurrentUser.HasFamilyContext = false;

        var response = await client.GetAsync("/api/lists");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("family_required", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WhenUnauthenticated_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        _factory.CurrentUser.IsAuthenticated = false;

        var response = await client.GetAsync("/api/lists");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("authentication_required", body.GetProperty("code").GetString());
    }

    // ---------- Idempotent create ----------

    [Fact]
    public async Task Create_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/lists", new { title = "Spesa", items = new[] { "Latte" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("idempotency_key_required", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_SameKeySamePayload_ReplaysPriorResult()
    {
        using var client = _factory.CreateClient();
        var key = Guid.NewGuid().ToString();

        var first = await CreateListAsync(client, "Spesa", ["Latte", "Pane"], key);
        var second = await CreateListAsync(client, "Spesa", ["Latte", "Pane"], key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var secondId = (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task Create_SameKeyDifferentPayload_ReturnsConflict()
    {
        using var client = _factory.CreateClient();
        var key = Guid.NewGuid().ToString();

        await CreateListAsync(client, "Spesa", ["Latte"], key);
        var conflict = await CreateListAsync(client, "Spesa", ["Latte", "Pane"], key);

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var body = await conflict.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("idempotency_conflict", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_PersistsListItemsAndIdempotencyRecordAtomically()
    {
        using var client = _factory.CreateClient();
        var before = _factory.Store.IdempotencyRecordCount;

        var created = await CreateListAsync(client, "Spesa", ["Latte", "Pane"], key: Guid.NewGuid().ToString());
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // List + items were persisted...
        var get = await client.GetAsync($"/api/lists/{id}");
        var detail = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, detail.GetProperty("items").GetArrayLength());

        // ...and exactly one idempotency record was written in the same operation.
        Assert.Equal(before + 1, _factory.Store.IdempotencyRecordCount);
    }

    // ---------- Validation of limits at the HTTP boundary ----------

    [Fact]
    public async Task Create_WithBlankTitle_ReturnsValidationError()
    {
        using var client = _factory.CreateClient();

        var response = await CreateListAsync(client, "   ", ["Latte"], key: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_WithTitleOverMaxLength_ReturnsValidationError()
    {
        using var client = _factory.CreateClient();
        var longTitle = new string('x', 101); // MaxTitleLength = 100

        var response = await CreateListAsync(client, longTitle, ["Latte"], key: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_WithTextOverMaxLength_ReturnsValidationError()
    {
        using var client = _factory.CreateClient();
        var created = await CreateListAsync(client, "Spesa", ["Latte"], key: Guid.NewGuid().ToString());
        var (id, etag) = await ReadIdAndEtagAsync(created);
        var longText = new string('y', 201); // MaxItemLength = 200

        var response = await PostWithIfMatchAsync(client, $"/api/lists/{id}/items", new { text = longText }, etag);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTooManyDraftItems_ReturnsValidationError()
    {
        using var client = _factory.CreateClient();
        var items = Enumerable.Range(0, 51).Select(i => $"Item {i}").ToArray(); // MaxItemsPerRecording/BulkConfirm = 50

        var response = await CreateListAsync(client, "Spesa", items, key: Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Audio operation endpoints ----------

    [Fact]
    public async Task CreateAudioOperation_ThenCompleteAndProcessNewList_ReturnsSucceededDraft()
    {
        using var client = _factory.CreateClient();
        _factory.AudioGenerator.Result = Kin.KinHub.KinList.Business.Common.Result<Kin.KinHub.KinList.Business.KinListFeature.ParsedKinListAudioDraft>.Success(
            new Kin.KinHub.KinList.Business.KinListFeature.ParsedKinListAudioDraft
            {
                Title = "Spesa",
                Items = ["Latte", "Pane"],
                DetectedLanguage = "it-IT",
                PromptVersion = "kinlist-audio-v1",
            });

        var createResponse = await client.PostAsJsonAsync("/api/audio-operations", new
        {
            type = "NewList",
            contentType = "audio/webm",
            declaredByteSize = 4,
        });

        Assert.Equal(HttpStatusCode.Accepted, createResponse.StatusCode);
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var operationId = createBody.GetProperty("id").GetGuid();
        var blobName = createBody.GetProperty("blobName").GetString()!;

        _factory.BlobStorage.Seed(blobName, [1, 2, 3, 4], "audio/webm");

        var completeResponse = await client.PostAsync($"/api/audio-operations/{operationId}/complete-upload", null);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        var queued = Assert.Single(_factory.AudioQueue.Messages);
        Assert.Equal(operationId, queued.OperationId);
        Assert.True(ActivityContext.TryParse(queued.CorrelationId, null, out _));

        var processingService = new KinListService(
            _factory.Store,
            _factory.Store,
            _factory.Store,
            _factory.Store,
            new TestKinListTransactionExecutor(),
            _factory.AudioGenerator,
            _factory.BlobStorage,
            _factory.AudioQueue,
            new KinListOptions());
        var processed = await processingService.ProcessAudioOperationAsync(operationId, CancellationToken.None);
        Assert.True(processed.IsSuccess);

        var getResponse = await client.GetAsync($"/api/audio-operations/{operationId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Succeeded", body.GetProperty("status").GetString());
        Assert.Equal("Spesa", body.GetProperty("title").GetString());
        Assert.Equal("it-IT", body.GetProperty("detectedLanguage").GetString());
    }

    [Fact]
    public async Task CreateAudioOperation_ReturnsLocationAndRetryAfterHeaders()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/audio-operations", new
        {
            type = "NewList",
            contentType = "audio/webm",
            declaredByteSize = 4,
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("2", response.Headers.RetryAfter?.Delta?.TotalSeconds.ToString("0"));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.EndsWith($"/api/audio-operations/{body.GetProperty("id").GetGuid():D}", response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateAndCompleteAudioOperation_DoNotWaitForAudioProcessing()
    {
        using var client = _factory.CreateClient();
        _factory.AudioGenerator.Reset();
        _factory.AudioGenerator.Delay = TimeSpan.FromSeconds(2);

        var createStopwatch = Stopwatch.StartNew();
        var createResponse = await client.PostAsJsonAsync("/api/audio-operations", new
        {
            type = "NewList",
            contentType = "audio/webm",
            declaredByteSize = 4,
        });
        createStopwatch.Stop();

        Assert.Equal(HttpStatusCode.Accepted, createResponse.StatusCode);
        Assert.True(createStopwatch.Elapsed < TimeSpan.FromMilliseconds(500), $"Create took {createStopwatch.Elapsed.TotalMilliseconds:0} ms.");
        Assert.Equal(0, _factory.AudioGenerator.CallCount);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var operationId = createBody.GetProperty("id").GetGuid();
        var blobName = createBody.GetProperty("blobName").GetString()!;
        _factory.BlobStorage.Seed(blobName, [1, 2, 3, 4], "audio/webm");

        var completeStopwatch = Stopwatch.StartNew();
        var completeResponse = await client.PostAsync($"/api/audio-operations/{operationId}/complete-upload", null);
        completeStopwatch.Stop();

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        Assert.True(completeStopwatch.Elapsed < TimeSpan.FromMilliseconds(500), $"Complete took {completeStopwatch.Elapsed.TotalMilliseconds:0} ms.");
        Assert.Equal(0, _factory.AudioGenerator.CallCount);

        var queued = Assert.Single(_factory.AudioQueue.Messages);
        Assert.Equal(operationId, queued.OperationId);
    }

    [Fact]
    public async Task CompleteAudioOperation_WhenBlobIsMissing_ReturnsValidationError()
    {
        using var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/audio-operations", new
        {
            type = "NewList",
            contentType = "audio/webm",
            declaredByteSize = 4,
        });

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var operationId = createBody.GetProperty("id").GetGuid();

        var completeResponse = await client.PostAsync($"/api/audio-operations/{operationId}/complete-upload", null);

        Assert.Equal(HttpStatusCode.BadRequest, completeResponse.StatusCode);
        var body = await completeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("audio_blob_missing", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetAudioOperation_WhenExpired_TransitionsToExpired()
    {
        using var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/audio-operations", new
        {
            type = "NewList",
            contentType = "audio/webm",
            declaredByteSize = 4,
        });

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var operationId = createBody.GetProperty("id").GetGuid();
        var operation = await _factory.Store.GetByIdAsync(operationId, CancellationToken.None);
        Assert.NotNull(operation);
        operation!.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await _factory.Store.UpdateAsync(operation, CancellationToken.None);

        var response = await client.GetAsync($"/api/audio-operations/{operationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Expired", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task DeleteAudioOperation_CancelsOperationAndDeletesBlob()
    {
        using var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/audio-operations", new
        {
            type = "NewList",
            contentType = "audio/webm",
            declaredByteSize = 4,
        });

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var operationId = createBody.GetProperty("id").GetGuid();
        var blobName = createBody.GetProperty("blobName").GetString()!;
        _factory.BlobStorage.Seed(blobName, [1, 2, 3, 4], "audio/webm");

        var deleteResponse = await client.DeleteAsync($"/api/audio-operations/{operationId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var operation = await _factory.Store.GetByIdAsync(operationId, CancellationToken.None);
        Assert.NotNull(operation);
        Assert.Equal("Cancelled", operation!.Status.ToString());
        Assert.Null(await _factory.BlobStorage.GetBlobAsync(blobName, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAudioOperation_WithAllowedMimeParameters_ReturnsAccepted()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/audio-operations", new
        {
            type = "NewList",
            contentType = "audio/webm;codecs=opus",
            declaredByteSize = 4,
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task CreateAudioOperation_WithDisallowedMime_ReturnsValidationError()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/audio-operations", new
        {
            type = "NewList",
            contentType = "audio/wav",
            declaredByteSize = 4,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("validation_error", body.GetProperty("code").GetString());
    }

    // ---------- helpers ----------

    private static async Task<HttpResponseMessage> CreateListAsync(HttpClient client, string title, string[] items, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/lists")
        {
            Content = JsonContent.Create(new { title, items }),
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PatchListAsync(HttpClient client, Guid id, string etag, string title)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/lists/{id}")
        {
            Content = JsonContent.Create(new { title }),
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PatchItemAsync(HttpClient client, Guid listId, Guid itemId, string etag, string text, bool isCompleted)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/lists/{listId}/items/{itemId}")
        {
            Content = JsonContent.Create(new { text, isCompleted }),
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostWithIfMatchAsync(HttpClient client, string url, object payload, string etag)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }

    private static async Task<(Guid Id, string ETag)> ReadIdAndEtagAsync(HttpResponseMessage response)
    {
        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (detail.GetProperty("id").GetGuid(), detail.GetProperty("eTag").GetString()!);
    }
}
