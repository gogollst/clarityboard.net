namespace ClarityBoard.Domain.Entities.Hr;

public class EmployeeAddressHistory
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string AddressType { get; private set; } = string.Empty;  // 'home' | 'work'
    public string Street { get; private set; } = string.Empty;
    public string HouseNumber { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string ChangeReason { get; private set; } = string.Empty;

    private EmployeeAddressHistory() { }

    public static EmployeeAddressHistory Create(Guid employeeId, string addressType, string street,
        string houseNumber, string postalCode, string city, string countryCode, Guid createdBy, string changeReason)
    => new()
    {
        Id           = Guid.NewGuid(),
        EmployeeId   = employeeId,
        AddressType  = addressType,
        Street       = street,
        HouseNumber  = houseNumber,
        PostalCode   = postalCode,
        City         = city,
        CountryCode  = countryCode,
        ValidFrom    = DateTime.UtcNow,
        CreatedBy    = createdBy,
        ChangeReason = changeReason,
    };

    public void Close(DateTime validTo) => ValidTo = validTo;
}
