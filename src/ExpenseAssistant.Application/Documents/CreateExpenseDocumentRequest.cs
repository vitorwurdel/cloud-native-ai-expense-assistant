namespace ExpenseAssistant.Application.Documents;

public sealed record CreateExpenseDocumentRequest(
    string Title,
    string Content,
    string Category,
    decimal Amount,
    string Currency
);