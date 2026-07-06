namespace Kin.KinHub.KinList.AzureStorage;

public sealed class AudioQueueMessage
{
    public int ContractVersion { get; set; } = 1;
    public required Guid OperationId { get; set; }
    public required string CorrelationId { get; set; }
}
