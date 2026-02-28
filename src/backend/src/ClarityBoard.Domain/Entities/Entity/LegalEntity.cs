namespace ClarityBoard.Domain.Entities.Entity;

public class LegalEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string LegalForm { get; private set; } = default!; // GmbH, AG, UG, etc.
    public string? RegistrationNumber { get; private set; }
    public string? TaxId { get; private set; }
    public string? VatId { get; private set; }
    public string Street { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string PostalCode { get; private set; } = default!;
    public string Country { get; private set; } = "DE";
    public string Currency { get; private set; } = "EUR";
    public string ChartOfAccounts { get; private set; } = "SKR03"; // SKR03 or SKR04
    public int FiscalYearStartMonth { get; private set; } = 1;
    public Guid? ParentEntityId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? DatevClientNumber { get; private set; } // DATEV Mandantennummer
    public string? DatevConsultantNumber { get; private set; } // DATEV Beraternummer
    public string? ManagingDirector { get; private set; } // Legacy free-text (kept for migration compatibility)
    public Guid? ManagingDirectorId { get; private set; } // FK to User (Geschäftsführer)
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private LegalEntity() { } // EF Core

    public static LegalEntity Create(
        string name,
        string legalForm,
        string street,
        string city,
        string postalCode,
        string chartOfAccounts = "SKR03",
        string currency = "EUR",
        string country = "DE",
        int fiscalYearStartMonth = 1,
        Guid? parentEntityId = null,
        string? registrationNumber = null,
        string? taxId = null,
        string? vatId = null,
        string? datevClientNumber = null,
        string? datevConsultantNumber = null,
        Guid? managingDirectorId = null)
    {
        return new LegalEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            LegalForm = legalForm,
            Street = street,
            City = city,
            PostalCode = postalCode,
            Country = country,
            ChartOfAccounts = chartOfAccounts,
            Currency = currency,
            FiscalYearStartMonth = fiscalYearStartMonth,
            ParentEntityId = parentEntityId,
            RegistrationNumber = registrationNumber,
            TaxId = taxId,
            VatId = vatId,
            DatevClientNumber = datevClientNumber,
            DatevConsultantNumber = datevConsultantNumber,
            ManagingDirectorId = managingDirectorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(
        string name,
        string legalForm,
        string street,
        string city,
        string postalCode,
        string country,
        string currency,
        string chartOfAccounts,
        int fiscalYearStartMonth,
        Guid? parentEntityId,
        string? registrationNumber,
        string? taxId,
        string? vatId,
        string? datevClientNumber,
        string? datevConsultantNumber,
        Guid? managingDirectorId)
    {
        Name = name;
        LegalForm = legalForm;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
        Currency = currency;
        ChartOfAccounts = chartOfAccounts;
        FiscalYearStartMonth = fiscalYearStartMonth;
        ParentEntityId = parentEntityId;
        RegistrationNumber = registrationNumber;
        TaxId = taxId;
        VatId = vatId;
        DatevClientNumber = datevClientNumber;
        DatevConsultantNumber = datevConsultantNumber;
        ManagingDirectorId = managingDirectorId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
