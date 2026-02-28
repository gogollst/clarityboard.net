namespace ClarityBoard.Domain.Entities.Accounting;

public class VatRecord
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid JournalEntryLineId { get; private set; }
    public short Year { get; private set; }
    public short Month { get; private set; }
    public string VatCode { get; private set; } = default!; // BU-Schlüssel
    public decimal VatRate { get; private set; }
    public decimal NetAmount { get; private set; }
    public decimal VatAmount { get; private set; }
    public string VatType { get; private set; } = default!; // output, input, reverse_charge, intra_eu
    public DateTime CreatedAt { get; private set; }

    private VatRecord() { }
}
