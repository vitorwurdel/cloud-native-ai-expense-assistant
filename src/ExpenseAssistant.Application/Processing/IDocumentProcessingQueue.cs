namespace ExpenseAssistant.Application.Processing;

public interface IDocumentProcessingQueue
{
    ValueTask EnqueueAsync(ProcessDocumentJob job, CancellationToken cancellationToken);

    ValueTask<ProcessDocumentJob> DequeueAsync(CancellationToken cancellationToken);
}