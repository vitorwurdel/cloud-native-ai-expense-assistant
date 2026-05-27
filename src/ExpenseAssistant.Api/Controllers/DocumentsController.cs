using ExpenseAssistant.Application.Documents;
using ExpenseAssistant.Application.Processing;
using ExpenseAssistant.Domain.Entities;
using ExpenseAssistant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseAssistant.Api.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IDocumentProcessingQueue _processingQueue;

    public DocumentsController(
        AppDbContext dbContext,
        IDocumentProcessingQueue processingQueue)
    {
        _dbContext = dbContext;
        _processingQueue = processingQueue;
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDocumentResponse>> Create(
        CreateExpenseDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var document = new ExpenseDocument(
            request.Title,
            request.Content,
            request.Category,
            request.Amount,
            request.Currency
        );

        _dbContext.ExpenseDocuments.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = document.Id },
            ToResponse(document)
        );
    }

    [HttpPost("{id:guid}/process")]
    public async Task<IActionResult> Process(
        Guid id,
        CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ExpenseDocuments
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!exists)
            return NotFound();

        await _processingQueue.EnqueueAsync(
            new ProcessDocumentJob(id),
            cancellationToken);

        return Accepted(new
        {
            documentId = id,
            status = "Queued for processing"
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExpenseDocumentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _dbContext.ExpenseDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (document is null)
            return NotFound();

        return Ok(ToResponse(document));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseDocumentResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var documents = await _dbContext.ExpenseDocuments
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(documents.Select(ToResponse).ToList());
    }

    private static ExpenseDocumentResponse ToResponse(ExpenseDocument document)
    {
        return new ExpenseDocumentResponse(
            document.Id,
            document.Title,
            document.Content,
            document.Category,
            document.Amount,
            document.Currency,
            document.IsProcessed,
            document.CreatedAtUtc
        );
    }
}