namespace ExpenseAssistant.Infrastructure.AI;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public string ChatModel { get; set; } = "gpt-4.1-mini";
}