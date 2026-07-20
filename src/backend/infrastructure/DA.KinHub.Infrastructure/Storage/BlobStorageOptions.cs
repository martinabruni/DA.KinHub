namespace DA.KinHub.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "Storage";

    public string AccountUri { get; init; } = string.Empty;
    public string ContainerName { get; init; } = "documents";
    public string? ConnectionString { get; init; }
}
