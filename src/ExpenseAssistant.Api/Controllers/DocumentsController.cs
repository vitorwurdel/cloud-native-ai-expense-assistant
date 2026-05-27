using ExpenseAssistant.Application.Common;
using ExpenseAssistant.Application.Documents;
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
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;

    public DocumentsController(
        AppDbContext dbContext,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
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
    public async Task<ActionResult<ExpenseDocumentResponse>> Process(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await _dbContext.ExpenseDocuments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (document is null)
            return NotFound();

        var searchableText = $"""
        Title: {document.Title}
        Category: {document.Category}
        Amount: {document.Amount} {document.Currency}
        Content: {document.Content}
        """;

        var embedding = await _embeddingService.GenerateEmbeddingAsync(
            searchableText,
            cancellationToken);

        await _vectorStoreService.UpsertDocumentEmbeddingAsync(
            document.Id,
            document.Title,
            embedding,
            cancellationToken);

        document.MarkAsProcessed();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(document));
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