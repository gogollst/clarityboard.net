namespace ClarityBoard.Domain.Entities.Document;

public class BookingSuggestion
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid DebitAccountId { get; private set; }
    public Guid CreditAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string? VatCode { get; private set; }
    public decimal? VatAmount { get; private set; }
    public string? Description { get; private set; }
    public decimal Confidence { get; private set; }
    public string Status { get; private set; } = "suggested"; // suggested, accepted, rejected, modified
    public string? AiReasoning { get; private set; } // JSON: AI explanation for suggestion
    public DateTime CreatedAt { get; private set; }
    public Guid? AcceptedBy { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    private BookingSuggestion() { }

    public static BookingSuggestion Create(
        Guid documentId, Guid entityId,
        Guid debitAccountId, Guid creditAccountId,
        decimal amount, string? vatCode, decimal? vatAmount,
        string? description, decimal confidence, string? aiReasoning = null)
    {
        return new BookingSuggestion
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            EntityId = entityId,
            DebitAccountId = debitAccountId,
            CreditAccountId = creditAccountId,
            Amount = amount,
            VatCode = vatCode,
            VatAmount = vatAmount,
            Description = description,
            Confidence = confidence,
            AiReasoning = aiReasoning,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Accept(Guid userId)
    {
        Status = "accepted";
        AcceptedBy = userId;
        AcceptedAt = DateTime.UtcNow;
    }

    public void Reject() => Status = "rejected";
}
