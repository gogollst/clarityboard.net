namespace ClarityBoard.Domain.Entities.Accounting;

public class JournalEntry
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public long EntryNumber { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public DateOnly PostingDate { get; private set; }
    public DateOnly? DocumentDate { get; private set; }
    public DateOnly? ServiceDate { get; private set; }
    public string Description { get; private set; } = default!;
    public string? DocumentRef { get; private set; }   // max 36 (DATEV Belegfeld 1)
    public string? DocumentRef2 { get; private set; }  // max 12 (DATEV Belegfeld 2)
    public string? Notes { get; private set; }
    public Guid? DocumentId { get; private set; }
    public Guid FiscalPeriodId { get; private set; }
    public string Status { get; private set; } = "draft"; // draft, posted, reversed
    public bool IsReversal { get; private set; }
    public Guid? ReversalOf { get; private set; }
    public string? SourceType { get; private set; }
    public string? SourceRef { get; private set; }
    public Guid? PartnerEntityId { get; private set; }
    public Guid? BusinessPartnerId { get; private set; }
    public string Hash { get; private set; } = default!;
    public string? PreviousHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public int Version { get; private set; } = 1;

    private readonly List<JournalEntryLine> _lines = new();
    public IReadOnlyCollection<JournalEntryLine> Lines => _lines.AsReadOnly();

    private JournalEntry() { } // EF Core

    public static JournalEntry Create(
        Guid entityId,
        long entryNumber,
        DateOnly entryDate,
        string description,
        Guid fiscalPeriodId,
        Guid createdBy,
        string? sourceType = null,
        string? sourceRef = null,
        Guid? documentId = null,
        string? documentRef = null,
        DateOnly? documentDate = null,
        DateOnly? serviceDate = null)
    {
        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntryNumber = entryNumber,
            EntryDate = entryDate,
            PostingDate = entryDate,
            DocumentDate = documentDate,
            ServiceDate = serviceDate,
            Description = description,
            DocumentRef = documentRef,
            FiscalPeriodId = fiscalPeriodId,
            CreatedBy = createdBy,
            SourceType = sourceType,
            SourceRef = sourceRef,
            DocumentId = documentId,
            CreatedAt = DateTime.UtcNow,
            Hash = string.Empty,
        };
    }

    public void AddLine(JournalEntryLine line)
    {
        _lines.Add(line);
    }

    public bool IsBalanced()
    {
        var totalDebit = _lines.Sum(l => l.DebitAmount);
        var totalCredit = _lines.Sum(l => l.CreditAmount);
        return totalDebit == totalCredit;
    }

    public void Post(string hash, string? previousHash)
    {
        if (!IsBalanced())
            throw new InvalidOperationException("Cannot post an unbalanced journal entry.");

        Status = "posted";
        Hash = hash;
        PreviousHash = previousHash;
    }

    public void MarkReversed(Guid reversalEntryId)
    {
        if (Status == "reversed")
            throw new InvalidOperationException("This journal entry has already been reversed.");
        Status = "reversed";
    }

    public void UpdateDraft(DateOnly entryDate, string description)
    {
        if (Status != "draft")
            throw new InvalidOperationException("Only draft journal entries can be edited.");
        EntryDate = entryDate;
        PostingDate = entryDate;
        Description = description;
    }

    public void ClearLines()
    {
        if (Status != "draft")
            throw new InvalidOperationException("Only draft journal entries can have lines modified.");
        _lines.Clear();
    }
}
