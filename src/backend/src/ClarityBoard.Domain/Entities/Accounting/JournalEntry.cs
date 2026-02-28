namespace ClarityBoard.Domain.Entities.Accounting;

public class JournalEntry
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public long EntryNumber { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public DateOnly PostingDate { get; private set; }
    public string Description { get; private set; } = default!;
    public Guid? DocumentId { get; private set; }
    public Guid FiscalPeriodId { get; private set; }
    public string Status { get; private set; } = "draft"; // draft, posted, reversed
    public bool IsReversal { get; private set; }
    public Guid? ReversalOf { get; private set; }
    public string? SourceType { get; private set; } // manual, webhook, recurring, ai-suggestion
    public string? SourceRef { get; private set; }
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
        Guid? documentId = null)
    {
        return new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntryNumber = entryNumber,
            EntryDate = entryDate,
            PostingDate = entryDate,
            Description = description,
            FiscalPeriodId = fiscalPeriodId,
            CreatedBy = createdBy,
            SourceType = sourceType,
            SourceRef = sourceRef,
            DocumentId = documentId,
            CreatedAt = DateTime.UtcNow,
            Hash = string.Empty, // Set after creation via ComputeHash
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
}
