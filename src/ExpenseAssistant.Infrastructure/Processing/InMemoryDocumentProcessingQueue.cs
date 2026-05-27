using System.Threading.Channels;
using ExpenseAssistant.Application.Processing;

namespace ExpenseAssistant.Infrastructure.Processing;

public sealed class InMemoryDocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly Channel<ProcessDocumentJob> _channel;

    public InMemoryDocumentProcessingQueue()
    {
        _channel = Channel.CreateUnbounded<ProcessDocumentJob>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async ValueTask EnqueueAsync(
        ProcessDocumentJob job,
        CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask<ProcessDocumentJob> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}