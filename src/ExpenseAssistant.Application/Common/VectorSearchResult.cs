namespace ExpenseAssistant.Application.Common;

public sealed record VectorSearchResult(
    Guid DocumentId,
    string Title,
    double Score
);