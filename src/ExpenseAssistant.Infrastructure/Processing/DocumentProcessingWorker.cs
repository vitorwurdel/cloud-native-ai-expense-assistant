using ExpenseAssistant.Application.Common;
using ExpenseAssistant.Application.Processing;
using ExpenseAssistant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExpenseAssistant.Infrastructure.Processing;

public sealed class DocumentProcessingWorker : BackgroundService
{
    private readonly IDocumentProcessingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(
        IDocumentProcessingQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentProcessingWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document processing worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);

                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing document job.");
            }
        }
    }

    private async Task ProcessJobAsync(
        ProcessDocumentJob job,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var vectorStoreService = scope.ServiceProvider.GetRequiredService<IVectorStoreService>();

        var document = await dbContext.ExpenseDocuments
            .FirstOrDefaultAsync(x => x.Id == job.DocumentId, cancellationToken);

        if (document is null)
        {
            _logger.LogWarning(
                "Document {DocumentId} was not found for processing.",
                job.DocumentId);

            return;
        }

        if (document.IsProcessed)
        {
            _logger.LogInformation(
                "Document {DocumentId} is already processed.",
                job.DocumentId);

            return;
        }

        var searchableText = $"""
        Title: {document.Title}
        Category: {document.Category}
        Amount: {document.Amount} {document.Currency}
        Content: {document.Content}
        """;

        _logger.LogInformation(
            "Generating embedding for document {DocumentId}.",
            document.Id);

        var embedding = await embeddingService.GenerateEmbeddingAsync(
            searchableText,
            cancellationToken);

        await vectorStoreService.UpsertDocumentEmbeddingAsync(
            document.Id,
            document.Title,
            embedding,
            cancellationToken);

        document.MarkAsProcessed();

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Document {DocumentId} processed successfully.",
            document.Id);
    }
}