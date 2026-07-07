namespace Kin.KinHub.KinList.Domain.KinListFeature;

public enum AudioProcessingOperationStatus
{
    AwaitingUpload = 1,
    Queued = 2,
    Processing = 3,
    Succeeded = 4,
    Failed = 5,
    Expired = 6,
    Cancelled = 7,
}
