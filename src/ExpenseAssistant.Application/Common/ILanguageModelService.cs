namespace ExpenseAssistant.Application.Common;

public interface ILanguageModelService
{
    Task<string> GenerateAnswerAsync(
        string question,
        string context,
        CancellationToken cancellationToken);
}