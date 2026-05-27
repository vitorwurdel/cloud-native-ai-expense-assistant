namespace ExpenseAssistant.Domain.Entities;

public class ExpenseDocument
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "USD";

    public bool IsProcessed { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private ExpenseDocument()
    {
    }

    public ExpenseDocument(
        string title,
        string content,
        string category,
        decimal amount,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required.", nameof(content));

        Title = title;
        Content = content;
        Category = category;
        Amount = amount;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency;
    }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
    }
}