using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExpenseAssistant.Application.Common;
using Microsoft.Extensions.Options;

namespace ExpenseAssistant.Infrastructure.AI;

public sealed class OpenAiLanguageModelService : ILanguageModelService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiLanguageModelService(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
    }

    public async Task<string> GenerateAnswerAsync(
        string question,
        string context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenAI API key was not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var userPrompt = $"""
        Answer the question using only the context below.

        If the answer cannot be found in the context, say:
        "I don't have enough information in the indexed documents to answer that."

        Context:
        {context}

        Question:
        {question}
        """;

        request.Content = JsonContent.Create(new
        {
            model = _options.ChatModel,
            temperature = 0.2,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are an assistant for travel, invoice and expense analysis. Be concise and cite the document titles when useful."
                },
                new
                {
                    role = "user",
                    content = userPrompt
                }
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI chat request failed: {responseContent}");

        using var json = JsonDocument.Parse(responseContent);

        return json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}