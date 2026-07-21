namespace DA.KinHub.Domain.Documents;

public sealed record StoredDocument(string Key, Uri Uri, string ContentType, long Length);

public interface IDocumentStorage
{
    Task<StoredDocument> SaveAsync(
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
