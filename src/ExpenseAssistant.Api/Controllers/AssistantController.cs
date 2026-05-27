using ExpenseAssistant.Application.Assistant;
using ExpenseAssistant.Application.Common;
using ExpenseAssistant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseAssistant.Api.Controllers;

[ApiController]
[Route("api/assistant")]
public class AssistantController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILanguageModelService _languageModelService;

    public AssistantController(
        AppDbContext dbContext,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        ILanguageModelService languageModelService)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _languageModelService = languageModelService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<AskQuestionResponse>> Ask(
        AskQuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(
            request.Question,
            cancellationToken);

        var searchResults = await _vectorStoreService.SearchAsync(
            questionEmbedding,
            limit: 5,
            cancellationToken);

        if (searchResults.Count == 0)
        {
            return Ok(new AskQuestionResponse(
                "I don't have enough information in the indexed documents to answer that.",
                Array.Empty<SourceDocumentResponse>()));
        }

        var documentIds = searchResults
            .Select(x => x.DocumentId)
            .ToList();

        var documents = await _dbContext.ExpenseDocuments
            .AsNoTracking()
            .Where(x => documentIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var orderedDocuments = searchResults
            .Join(
                documents,
                search => search.DocumentId,
                document => document.Id,
                (search, document) => new
                {
                    Search = search,
                    Document = document
                })
            .ToList();

        var context = string.Join(
            "\n\n---\n\n",
            orderedDocuments.Select(x =>
                $"""
                Document title: {x.Document.Title}
                Category: {x.Document.Category}
                Amount: {x.Document.Amount} {x.Document.Currency}
                Content: {x.Document.Content}
                """));

        var answer = await _languageModelService.GenerateAnswerAsync(
            request.Question,
            context,
            cancellationToken);

        var sources = orderedDocuments
            .Select(x => new SourceDocumentResponse(
                x.Document.Id,
                x.Document.Title,
                x.Search.Score))
            .ToList();

        return Ok(new AskQuestionResponse(answer, sources));
    }
}