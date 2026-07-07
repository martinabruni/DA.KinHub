using Kin.KinHub.KinList.Domain.KinListFeature;
using Kin.KinHub.KinList.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Kin.KinHub.KinList.PostgreSql.KinListFeature;

public sealed class AudioProcessingOperationRepository : IAudioProcessingOperationRepository
{
    private readonly KinListDbContext _context;

    public AudioProcessingOperationRepository(KinListDbContext context)
    {
        _context = context;
    }

    public async Task<AudioProcessingOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AudioProcessingOperations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<AudioProcessingOperation> AddAsync(AudioProcessingOperation operation, CancellationToken cancellationToken = default)
    {
        var entity = Map(operation);
        _context.AudioProcessingOperations.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AudioProcessingOperation> UpdateAsync(AudioProcessingOperation operation, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AudioProcessingOperations.FirstOrDefaultAsync(x => x.Id == operation.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Audio processing operation '{operation.Id}' was not found.");

        entity.Type = (int)operation.Type;
        entity.ListId = operation.ListId;
        entity.Status = (int)operation.Status;
        entity.BlobName = operation.BlobName;
        entity.ContentType = operation.ContentType;
        entity.DeclaredByteSize = operation.DeclaredByteSize;
        entity.UploadedByteSize = operation.UploadedByteSize;
        entity.Title = operation.Title;
        entity.ProposedItemsJson = operation.ProposedItemsJson;
        entity.DetectedLanguage = operation.DetectedLanguage;
        entity.PromptVersion = operation.PromptVersion;
        entity.ErrorCode = operation.ErrorCode;
        entity.ErrorMessage = operation.ErrorMessage;
        entity.AttemptCount = operation.AttemptCount;
        entity.CorrelationId = operation.CorrelationId;
        entity.Version = operation.Version;
        entity.UpdatedAt = operation.UpdatedAt;
        entity.ExpiresAt = operation.ExpiresAt;
        entity.UploadCompletedAt = operation.UploadCompletedAt;
        entity.ProcessingStartedAt = operation.ProcessingStartedAt;
        entity.CompletedAt = operation.CompletedAt;
        entity.LastHeartbeatAt = operation.LastHeartbeatAt;

        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AudioProcessingOperation?> TryStartProcessingAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var startedVersion = Guid.NewGuid();
        var updatedRows = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE kinlist."AudioProcessingOperation"
            SET "Status" = {(int)AudioProcessingOperationStatus.Processing},
                "AttemptCount" = "AttemptCount" + 1,
                "ProcessingStartedAt" = COALESCE("ProcessingStartedAt", {utcNow}),
                "LastHeartbeatAt" = {utcNow},
                "UpdatedAt" = {utcNow},
                "Version" = {startedVersion}
            WHERE "Id" = {id}
              AND "Status" = {(int)AudioProcessingOperationStatus.Queued}
            """,
            cancellationToken);

        if (updatedRows is 0)
        {
            return null;
        }

        var entity = await _context.AudioProcessingOperations.AsNoTracking().FirstAsync(x => x.Id == id, cancellationToken);
        return Map(entity);
    }

    private static AudioProcessingOperation Map(AudioProcessingOperationEntity entity) =>
        new()
        {
            Id = entity.Id,
            FamilyId = entity.FamilyId,
            UserId = entity.UserId,
            Type = (AudioProcessingOperationType)entity.Type,
            ListId = entity.ListId,
            Status = (AudioProcessingOperationStatus)entity.Status,
            BlobName = entity.BlobName,
            ContentType = entity.ContentType,
            DeclaredByteSize = entity.DeclaredByteSize,
            UploadedByteSize = entity.UploadedByteSize,
            Title = entity.Title,
            ProposedItemsJson = entity.ProposedItemsJson,
            DetectedLanguage = entity.DetectedLanguage,
            PromptVersion = entity.PromptVersion,
            ErrorCode = entity.ErrorCode,
            ErrorMessage = entity.ErrorMessage,
            AttemptCount = entity.AttemptCount,
            CorrelationId = entity.CorrelationId,
            Version = entity.Version,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            ExpiresAt = entity.ExpiresAt,
            UploadCompletedAt = entity.UploadCompletedAt,
            ProcessingStartedAt = entity.ProcessingStartedAt,
            CompletedAt = entity.CompletedAt,
            LastHeartbeatAt = entity.LastHeartbeatAt,
        };

    private static AudioProcessingOperationEntity Map(AudioProcessingOperation operation) =>
        new()
        {
            Id = operation.Id,
            FamilyId = operation.FamilyId,
            UserId = operation.UserId,
            Type = (int)operation.Type,
            ListId = operation.ListId,
            Status = (int)operation.Status,
            BlobName = operation.BlobName,
            ContentType = operation.ContentType,
            DeclaredByteSize = operation.DeclaredByteSize,
            UploadedByteSize = operation.UploadedByteSize,
            Title = operation.Title,
            ProposedItemsJson = operation.ProposedItemsJson,
            DetectedLanguage = operation.DetectedLanguage,
            PromptVersion = operation.PromptVersion,
            ErrorCode = operation.ErrorCode,
            ErrorMessage = operation.ErrorMessage,
            AttemptCount = operation.AttemptCount,
            CorrelationId = operation.CorrelationId,
            Version = operation.Version,
            CreatedAt = operation.CreatedAt,
            UpdatedAt = operation.UpdatedAt,
            ExpiresAt = operation.ExpiresAt,
            UploadCompletedAt = operation.UploadCompletedAt,
            ProcessingStartedAt = operation.ProcessingStartedAt,
            CompletedAt = operation.CompletedAt,
            LastHeartbeatAt = operation.LastHeartbeatAt,
        };
}
