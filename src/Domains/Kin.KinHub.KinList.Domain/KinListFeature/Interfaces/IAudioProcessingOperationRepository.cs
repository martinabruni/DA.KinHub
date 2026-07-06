namespace Kin.KinHub.KinList.Domain.KinListFeature;

public interface IAudioProcessingOperationRepository
{
    Task<AudioProcessingOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AudioProcessingOperation> AddAsync(AudioProcessingOperation operation, CancellationToken cancellationToken = default);
    Task<AudioProcessingOperation> UpdateAsync(AudioProcessingOperation operation, CancellationToken cancellationToken = default);
    Task<AudioProcessingOperation?> TryStartProcessingAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default);
}
