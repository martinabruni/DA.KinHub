using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Kin.KinHub.KinList.AzureStorage;

namespace Kin.KinHub.Core.Test;

public sealed class KinListAzureStorageAzuriteIntegrationTests : IClassFixture<AzuriteFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AzuriteFixture _azurite;

    public KinListAzureStorageAzuriteIntegrationTests(AzuriteFixture azurite)
    {
        _azurite = azurite;
    }

    [Fact]
    public async Task BlobStorage_WithAzurite_CreatesScopedUploadTargetAndRoundTripsBlob()
    {
        var options = CreateOptions();
        var clients = new AzureStorageAudioClients(options);
        var storage = new AzureBlobAudioProcessingBlobStorage(clients);
        await clients.ContainerClient.CreateIfNotExistsAsync();

        var target = await storage.CreateUploadTargetAsync(
            blobName: "family-a/op-1",
            contentType: "audio/webm",
            timeToLive: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        var permissions = GetQueryParameter(target.UploadUrl, "sp");
        Assert.Equal("cw", permissions);

        var uploadClient = new BlobClient(target.UploadUrl);
        await uploadClient.UploadAsync(
            BinaryData.FromBytes([1, 2, 3, 4]),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "audio/webm",
                },
            },
            CancellationToken.None);

        var descriptor = await storage.GetBlobAsync(target.BlobName, CancellationToken.None);
        Assert.NotNull(descriptor);
        Assert.Equal("audio/webm", descriptor!.ContentType);
        Assert.Equal(4, descriptor.ContentLength);

        await using var stream = await storage.OpenReadAsync(target.BlobName, CancellationToken.None);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        Assert.Equal([1, 2, 3, 4], memory.ToArray());

        await storage.DeleteIfExistsAsync(target.BlobName, CancellationToken.None);
        Assert.Null(await storage.GetBlobAsync(target.BlobName, CancellationToken.None));
    }

    [Fact]
    public async Task QueueStorage_WithAzurite_EnqueuesReceivesRenewsPoisonsAndDeletesMessages()
    {
        var options = CreateOptions();
        var clients = new AzureStorageAudioClients(options);
        var queue = new AzureQueueAudioProcessingQueue(clients);
        var pump = new AzureAudioProcessingQueuePump(clients);

        await pump.InitializeAsync(CancellationToken.None);
        Assert.True((await clients.ContainerClient.ExistsAsync()).Value);
        Assert.True((await clients.ProcessingQueueClient.ExistsAsync()).Value);
        Assert.True((await clients.PoisonQueueClient.ExistsAsync()).Value);

        var operationId = Guid.NewGuid();
        await queue.EnqueueAsync(operationId, "corr-1", CancellationToken.None);

        var messages = await pump.ReceiveMessagesAsync(1, TimeSpan.FromSeconds(30), CancellationToken.None);
        var message = Assert.Single(messages);
        var payload = JsonSerializer.Deserialize<AudioQueueMessage>(message.MessageText, JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(operationId, payload!.OperationId);
        Assert.Equal("corr-1", payload.CorrelationId);

        var originalPopReceipt = message.PopReceipt;
        await pump.RenewMessageVisibilityAsync(message, TimeSpan.FromSeconds(30), CancellationToken.None);
        Assert.NotEqual(originalPopReceipt, message.PopReceipt);

        await pump.SendPoisonMessageAsync(message.MessageText, CancellationToken.None);
        var poisonMessages = (await clients.PoisonQueueClient.ReceiveMessagesAsync(maxMessages: 1)).Value;
        var poisonPayload = JsonSerializer.Deserialize<AudioQueueMessage>(Assert.Single(poisonMessages).MessageText, JsonOptions);
        Assert.NotNull(poisonPayload);
        Assert.Equal(operationId, poisonPayload!.OperationId);

        await pump.DeleteMessageAsync(message, CancellationToken.None);
        var remaining = await pump.ReceiveMessagesAsync(1, TimeSpan.FromSeconds(1), CancellationToken.None);
        Assert.Empty(remaining);
    }

    private AudioStorageOptions CreateOptions()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return new AudioStorageOptions
        {
            ConnectionString = _azurite.ConnectionString,
            ContainerName = $"kinlist-audio-{suffix}",
            ProcessingQueueName = $"kinlist-proc-{suffix}",
            PoisonQueueName = $"kinlist-poison-{suffix}",
        };
    }

    private static string GetQueryParameter(Uri uri, string key)
    {
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(WebUtility.UrlDecode(parts[0]), key, StringComparison.Ordinal))
            {
                return WebUtility.UrlDecode(parts[1]);
            }
        }

        throw new InvalidOperationException($"Query parameter '{key}' was not found.");
    }
}

public sealed class AzuriteFixture : IAsyncLifetime, IDisposable
{
    private readonly string _accountName = "kinlisttest";
    private readonly string _accountKey;
    private readonly int _blobPort;
    private readonly int _queuePort;
    private readonly int _tablePort;
    private readonly string _location;
    private Process? _process;

    public AzuriteFixture()
    {
        _accountKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"));
        _blobPort = GetFreePort();
        _queuePort = GetFreePort();
        _tablePort = GetFreePort();
        _location = Path.Combine(Path.GetTempPath(), "kinhub-azurite", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_location);
        ConnectionString = $"DefaultEndpointsProtocol=http;AccountName={_accountName};AccountKey={_accountKey};BlobEndpoint=http://127.0.0.1:{_blobPort}/{_accountName};QueueEndpoint=http://127.0.0.1:{_queuePort}/{_accountName};TableEndpoint=http://127.0.0.1:{_tablePort}/{_accountName};";
    }

    public string ConnectionString { get; }

    public async Task InitializeAsync()
    {
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = FindRepositoryRoot(),
        };

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c npx -y azurite --silent --location \"{_location}\" --blobHost 127.0.0.1 --blobPort {_blobPort} --queueHost 127.0.0.1 --queuePort {_queuePort} --tableHost 127.0.0.1 --tablePort {_tablePort}";
        }
        else
        {
            startInfo.FileName = "npx";
            startInfo.Arguments = $"-y azurite --silent --location \"{_location}\" --blobHost 127.0.0.1 --blobPort {_blobPort} --queueHost 127.0.0.1 --queuePort {_queuePort} --tableHost 127.0.0.1 --tablePort {_tablePort}";
        }

        startInfo.Environment["AZURITE_ACCOUNTS"] = $"{_accountName}:{_accountKey}";

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Azurite.");
        await WaitUntilReadyAsync();
    }

    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
        }
        catch
        {
        }

        try
        {
            if (Directory.Exists(_location))
            {
                Directory.Delete(_location, recursive: true);
            }
        }
        catch
        {
        }
    }

    private async Task WaitUntilReadyAsync()
    {
        var blobServiceClient = new BlobServiceClient(ConnectionString);
        var lastError = (Exception?)null;

        for (var attempt = 0; attempt < 40; attempt++)
        {
            if (_process is { HasExited: true })
            {
                var stderr = await _process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Azurite exited before becoming ready. {stderr}".Trim());
            }

            try
            {
                await blobServiceClient.GetPropertiesAsync();
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                await Task.Delay(250);
            }
        }

        throw new InvalidOperationException("Azurite did not become ready in time.", lastError);
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Kin.KinHub.Core.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
