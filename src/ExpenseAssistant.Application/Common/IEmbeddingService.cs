namespace ExpenseAssistant.Application.Common;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken);
}