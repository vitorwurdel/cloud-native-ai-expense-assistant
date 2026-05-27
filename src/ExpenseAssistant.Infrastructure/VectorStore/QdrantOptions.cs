namespace ExpenseAssistant.Infrastructure.VectorStore;

public sealed class QdrantOptions
{
    public string Url { get; set; } = "http://localhost:6333";

    public string CollectionName { get; set; } = "expense_documents";

    public int VectorSize { get; set; } = 1536;
}