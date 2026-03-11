namespace ClarityBoard.Domain.Entities.Accounting;

public class LegalEntityExtension
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }  // FK to public.legal_entities
    public string? DatevConsultantNumber { get; private set; }
    public string? DatevClientNumber { get; private set; }
    public string? HrbNumber { get; private set; }
    public string? VatId { get; private set; }
    public string? TaxOffice { get; private set; }
    public string? DefaultCurrencyCode { get; private set; } = "EUR";
    public string? FiscalYearStartMonth { get; private set; } = "01";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private LegalEntityExtension() { }

    public static LegalEntityExtension Create(Guid entityId)
    {
        return new LegalEntityExtension
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string? datevConsultantNumber, string? datevClientNumber,
        string? hrbNumber, string? vatId, string? taxOffice,
        string? defaultCurrencyCode, string? fiscalYearStartMonth)
    {
        DatevConsultantNumber = datevConsultantNumber;
        DatevClientNumber = datevClientNumber;
        HrbNumber = hrbNumber;
        VatId = vatId;
        TaxOffice = taxOffice;
        DefaultCurrencyCode = defaultCurrencyCode ?? "EUR";
        FiscalYearStartMonth = fiscalYearStartMonth ?? "01";
        UpdatedAt = DateTime.UtcNow;
    }
}
