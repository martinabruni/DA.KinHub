namespace Kin.KinHub.KinList.Business.Common;

public sealed class KinListOptions
{
    public const string SectionName = "KinList";

    public int MaxTitleLength { get; set; } = 100;
    public int MaxItemLength { get; set; } = 200;
    public int MaxItemsPerList { get; set; } = 100;
    public int MaxItemsPerBulkConfirm { get; set; } = 50;
    public int IdempotencyRetentionHours { get; set; } = 24;
    public int MaxAudioDurationSeconds { get; set; } = 60;
    public long MaxAudioBytes { get; set; } = 10 * 1024 * 1024;
    public int AudioProcessingTimeoutSeconds { get; set; } = 30;
    public int TransientRetryMaxAttempts { get; set; } = 3;
    public int TransientRetryBaseDelayMilliseconds { get; set; } = 250;
    public int TransientRetryMaxDelayMilliseconds { get; set; } = 5000;
    public int IdempotencyCleanupIntervalMinutes { get; set; } = 60;
    public string[] AllowedAudioMimeTypes { get; set; } =
    [
        "audio/webm",
        "video/webm",
        "audio/mp4",
        "audio/x-m4a",
        "audio/m4a",
        "audio/ogg",
        "application/ogg",
    ];

    public void Validate()
    {
        if (MaxTitleLength <= 0)
        {
            throw new InvalidOperationException("KinList:MaxTitleLength must be greater than zero.");
        }

        if (MaxItemLength <= 0)
        {
            throw new InvalidOperationException("KinList:MaxItemLength must be greater than zero.");
        }

        if (MaxItemsPerList <= 0)
        {
            throw new InvalidOperationException("KinList:MaxItemsPerList must be greater than zero.");
        }

        if (MaxItemsPerBulkConfirm <= 0)
        {
            throw new InvalidOperationException("KinList:MaxItemsPerBulkConfirm must be greater than zero.");
        }

        if (MaxItemsPerBulkConfirm > MaxItemsPerList)
        {
            throw new InvalidOperationException("KinList:MaxItemsPerBulkConfirm cannot exceed KinList:MaxItemsPerList.");
        }

        if (IdempotencyRetentionHours <= 0)
        {
            throw new InvalidOperationException("KinList:IdempotencyRetentionHours must be greater than zero.");
        }

        if (MaxAudioDurationSeconds <= 0)
        {
            throw new InvalidOperationException("KinList:MaxAudioDurationSeconds must be greater than zero.");
        }

        if (MaxAudioBytes <= 0)
        {
            throw new InvalidOperationException("KinList:MaxAudioBytes must be greater than zero.");
        }

        if (AudioProcessingTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("KinList:AudioProcessingTimeoutSeconds must be greater than zero.");
        }

        if (TransientRetryMaxAttempts <= 0)
        {
            throw new InvalidOperationException("KinList:TransientRetryMaxAttempts must be greater than zero.");
        }

        if (TransientRetryBaseDelayMilliseconds <= 0)
        {
            throw new InvalidOperationException("KinList:TransientRetryBaseDelayMilliseconds must be greater than zero.");
        }

        if (TransientRetryMaxDelayMilliseconds < TransientRetryBaseDelayMilliseconds)
        {
            throw new InvalidOperationException("KinList:TransientRetryMaxDelayMilliseconds cannot be less than KinList:TransientRetryBaseDelayMilliseconds.");
        }

        if (IdempotencyCleanupIntervalMinutes <= 0)
        {
            throw new InvalidOperationException("KinList:IdempotencyCleanupIntervalMinutes must be greater than zero.");
        }

        if (AllowedAudioMimeTypes.Length is 0 || AllowedAudioMimeTypes.Any(x => string.IsNullOrWhiteSpace(x)))
        {
            throw new InvalidOperationException("KinList:AllowedAudioMimeTypes must contain at least one MIME type.");
        }
    }
}
