namespace ExpenseAssistant.Application.Documents;

public sealed record ExpenseDocumentResponse(
    Guid Id,
    string Title,
    string Content,
    string Category,
    decimal Amount,
    string Currency,
    bool IsProcessed,
    DateTime CreatedAtUtc
);