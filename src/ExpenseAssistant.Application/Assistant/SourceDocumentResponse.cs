namespace ExpenseAssistant.Application.Assistant;

public sealed record SourceDocumentResponse(
    Guid DocumentId,
    string Title,
    double Score
);