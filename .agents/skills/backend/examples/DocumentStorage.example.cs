using DA.KinHub.Domain.Documents;

public static class DocumentStorageExample
{
    public static async Task<StoredDocument> SaveTextAsync(
        IDocumentStorage storage,
        string text,
        CancellationToken cancellationToken)
    {
        await using var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        return await storage.SaveAsync("note.txt", "text/plain", content, cancellationToken);
    }
}
