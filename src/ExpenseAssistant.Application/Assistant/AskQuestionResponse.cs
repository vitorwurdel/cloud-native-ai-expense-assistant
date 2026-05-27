namespace ExpenseAssistant.Application.Assistant;

public sealed record AskQuestionResponse(
    string Answer,
    IReadOnlyList<SourceDocumentResponse> Sources
);