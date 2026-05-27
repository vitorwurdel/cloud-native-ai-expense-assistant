using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExpenseAssistant.Application.Common;
using Microsoft.Extensions.Options;

namespace ExpenseAssistant.Infrastructure.AI;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiEmbeddingService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input is required.", nameof(input));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenAI API key was not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        request.Content = JsonContent.Create(new
        {
            model = _options.EmbeddingModel,
            input,
            encoding_format = "float"
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI embeddings request failed: {responseContent}");

        using var json = JsonDocument.Parse(responseContent);

        var embeddingElement = json.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding");

        return embeddingElement
            .EnumerateArray()
            .Select(x => x.GetSingle())
            .ToArray();
    }
}