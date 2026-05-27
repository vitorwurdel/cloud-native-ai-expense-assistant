namespace ExpenseAssistant.Application.Common;

public interface IVectorStoreService
{
    Task EnsureCollectionAsync(CancellationToken cancellationToken);

    Task UpsertDocumentEmbeddingAsync(
        Guid documentId,
        string title,
        float[] vector,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] vector,
        int limit,
        CancellationToken cancellationToken);
}