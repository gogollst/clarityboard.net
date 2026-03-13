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
    public Guid? HrEmployeeId { get; private set; }
    public Guid? ModifiedBy { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public Guid? RejectedBy { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public bool IsAutoBooked { get; private set; }
    public string? InvoiceType { get; private set; }
    public string? TaxKey { get; private set; }
    public string? VatTreatmentType { get; private set; }
    public Guid? SuggestedEntityId { get; private set; }

    private BookingSuggestion() { }

    public static BookingSuggestion Create(
        Guid documentId, Guid entityId,
        Guid debitAccountId, Guid creditAccountId,
        decimal amount, string? vatCode, decimal? vatAmount,
        string? description, decimal confidence, string? aiReasoning = null,
        string? invoiceType = null, string? taxKey = null, string? vatTreatmentType = null,
        Guid? suggestedEntityId = null)
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
            InvoiceType = invoiceType,
            TaxKey = taxKey,
            VatTreatmentType = vatTreatmentType,
            SuggestedEntityId = suggestedEntityId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Accept(Guid userId, Guid? hrEmployeeId = null)
    {
        Status = "accepted";
        AcceptedBy = userId;
        AcceptedAt = DateTime.UtcNow;
        HrEmployeeId = hrEmployeeId;
    }

    public void Modify(Guid userId, Guid debitAccountId, Guid creditAccountId,
        decimal amount, string? vatCode, decimal? vatAmount, string? description, Guid? hrEmployeeId)
    {
        DebitAccountId = debitAccountId;
        CreditAccountId = creditAccountId;
        Amount = amount;
        VatCode = vatCode;
        VatAmount = vatAmount;
        Description = description;
        HrEmployeeId = hrEmployeeId;
        Status = "modified";
        ModifiedBy = userId;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Reject(Guid userId, string? reason)
    {
        Status = "rejected";
        RejectedBy = userId;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = reason;
    }

    public void SetSuggestedEntity(Guid? entityId)
    {
        SuggestedEntityId = entityId;
    }

    public void MarkAutoBooked()
    {
        IsAutoBooked = true;
    }
}
