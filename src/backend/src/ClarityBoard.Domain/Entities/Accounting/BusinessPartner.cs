namespace ClarityBoard.Domain.Entities.Accounting;

public class BusinessPartner
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string PartnerNumber { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? TaxId { get; private set; }
    public string? VatNumber { get; private set; }
    public string? Street { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Country { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? BankName { get; private set; }
    public string? Iban { get; private set; }
    public string? Bic { get; private set; }
    public bool IsCreditor { get; private set; }
    public bool IsDebtor { get; private set; }
    public Guid? DefaultExpenseAccountId { get; private set; }
    public Guid? DefaultRevenueAccountId { get; private set; }
    public Guid? ContactEmployeeId { get; private set; }
    public int PaymentTermDays { get; private set; } = 30;
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private BusinessPartner() { } // EF Core

    public static BusinessPartner Create(
        Guid entityId,
        string name,
        bool isCreditor,
        bool isDebtor,
        string? taxId = null,
        string? vatNumber = null,
        string? street = null,
        string? city = null,
        string? postalCode = null,
        string? country = null,
        string? email = null,
        string? phone = null,
        string? bankName = null,
        string? iban = null,
        string? bic = null,
        Guid? defaultExpenseAccountId = null,
        Guid? defaultRevenueAccountId = null,
        Guid? contactEmployeeId = null,
        int paymentTermDays = 30,
        string? notes = null)
    {
        return new BusinessPartner
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            PartnerNumber = string.Empty, // Set via SetPartnerNumber after creation
            Name = name,
            IsCreditor = isCreditor,
            IsDebtor = isDebtor,
            TaxId = taxId,
            VatNumber = vatNumber,
            Street = street,
            City = city,
            PostalCode = postalCode,
            Country = country,
            Email = email,
            Phone = phone,
            BankName = bankName,
            Iban = iban,
            Bic = bic,
            DefaultExpenseAccountId = defaultExpenseAccountId,
            DefaultRevenueAccountId = defaultRevenueAccountId,
            ContactEmployeeId = contactEmployeeId,
            PaymentTermDays = paymentTermDays,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void SetPartnerNumber(string partnerNumber)
    {
        PartnerNumber = partnerNumber;
    }

    public void Update(
        string name,
        bool isCreditor,
        bool isDebtor,
        string? taxId,
        string? vatNumber,
        string? street,
        string? city,
        string? postalCode,
        string? country,
        string? email,
        string? phone,
        string? bankName,
        string? iban,
        string? bic,
        Guid? defaultExpenseAccountId,
        Guid? defaultRevenueAccountId,
        Guid? contactEmployeeId,
        int paymentTermDays,
        string? notes)
    {
        Name = name;
        IsCreditor = isCreditor;
        IsDebtor = isDebtor;
        TaxId = taxId;
        VatNumber = vatNumber;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
        Email = email;
        Phone = phone;
        BankName = bankName;
        Iban = iban;
        Bic = bic;
        DefaultExpenseAccountId = defaultExpenseAccountId;
        DefaultRevenueAccountId = defaultRevenueAccountId;
        ContactEmployeeId = contactEmployeeId;
        PaymentTermDays = paymentTermDays;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Fills in fields that are currently null with new values from extraction.
    /// Never overwrites existing data. Returns true if any field was updated.
    /// </summary>
    public bool EnrichMissingFields(
        string? taxId = null,
        string? vatNumber = null,
        string? street = null,
        string? city = null,
        string? postalCode = null,
        string? country = null,
        string? email = null,
        string? phone = null,
        string? bankName = null,
        string? iban = null,
        string? bic = null)
    {
        var changed = false;

        if (TaxId is null && taxId is not null) { TaxId = taxId; changed = true; }
        if (VatNumber is null && vatNumber is not null) { VatNumber = vatNumber; changed = true; }
        if (Street is null && street is not null) { Street = street; changed = true; }
        if (City is null && city is not null) { City = city; changed = true; }
        if (PostalCode is null && postalCode is not null) { PostalCode = postalCode; changed = true; }
        if (Country is null && country is not null) { Country = country; changed = true; }
        if (Email is null && email is not null) { Email = email; changed = true; }
        if (Phone is null && phone is not null) { Phone = phone; changed = true; }
        if (BankName is null && bankName is not null) { BankName = bankName; changed = true; }
        if (Iban is null && iban is not null) { Iban = iban; changed = true; }
        if (Bic is null && bic is not null) { Bic = bic; changed = true; }

        if (changed)
            UpdatedAt = DateTime.UtcNow;

        return changed;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
