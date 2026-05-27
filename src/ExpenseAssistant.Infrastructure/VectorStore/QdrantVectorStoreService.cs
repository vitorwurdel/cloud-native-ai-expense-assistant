using System.Net.Http.Json;
using System.Text.Json;
using ExpenseAssistant.Application.Common;
using Microsoft.Extensions.Options;

namespace ExpenseAssistant.Infrastructure.VectorStore;

public sealed class QdrantVectorStoreService : IVectorStoreService
{
    private readonly HttpClient _httpClient;
    private readonly QdrantOptions _options;

    public QdrantVectorStoreService(
        HttpClient httpClient,
        IOptions<QdrantOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(_options.Url);
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        var collectionUrl = $"/collections/{_options.CollectionName}";

        using var getResponse = await _httpClient.GetAsync(collectionUrl, cancellationToken);

        if (getResponse.IsSuccessStatusCode)
            return;

        var createPayload = new
        {
            vectors = new
            {
                size = _options.VectorSize,
                distance = "Cosine"
            }
        };

        using var createResponse = await _httpClient.PutAsJsonAsync(
            collectionUrl,
            createPayload,
            cancellationToken);

        var createContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!createResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Qdrant collection creation failed: {createContent}");
    }

    public async Task UpsertDocumentEmbeddingAsync(
        Guid documentId,
        string title,
        float[] vector,
        CancellationToken cancellationToken)
    {
        await EnsureCollectionAsync(cancellationToken);

        var payload = new
        {
            points = new[]
            {
                new
                {
                    id = documentId,
                    vector,
                    payload = new
                    {
                        documentId = documentId.ToString(),
                        title
                    }
                }
            }
        };

        using var response = await _httpClient.PutAsJsonAsync(
            $"/collections/{_options.CollectionName}/points?wait=true",
            payload,
            cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Qdrant upsert failed: {responseContent}");
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] vector,
        int limit,
        CancellationToken cancellationToken)
    {
        await EnsureCollectionAsync(cancellationToken);

        var searchPayload = new
        {
            vector,
            limit,
            with_payload = true
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"/collections/{_options.CollectionName}/points/search",
            searchPayload,
            cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Qdrant search failed: {responseContent}");

        using var json = JsonDocument.Parse(responseContent);

        var results = new List<VectorSearchResult>();

        if (!json.RootElement.TryGetProperty("result", out var resultElement))
            return results;

        foreach (var item in resultElement.EnumerateArray())
        {
            var score = item.GetProperty("score").GetDouble();

            var payload = item.GetProperty("payload");

            var documentIdText = payload.GetProperty("documentId").GetString();
            var title = payload.GetProperty("title").GetString() ?? string.Empty;

            if (!Guid.TryParse(documentIdText, out var documentId))
                continue;

            results.Add(new VectorSearchResult(documentId, title, score));
        }

        return results;
    }
}