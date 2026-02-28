namespace ClarityBoard.Domain.Entities.Accounting;

public class Account
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string AccountNumber { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string AccountType { get; private set; } = default!; // asset, liability, equity, revenue, expense
    public short AccountClass { get; private set; } // HGB class (0-9)
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? VatDefault { get; private set; }
    public string? DatevAuto { get; private set; }
    public string? CostCenterDefault { get; private set; }
    public bool IsSystemAccount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Account() { } // EF Core

    public static Account Create(
        Guid entityId,
        string accountNumber,
        string name,
        string accountType,
        short accountClass,
        Guid? parentId = null,
        string? vatDefault = null,
        string? datevAuto = null)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            AccountNumber = accountNumber,
            Name = name,
            AccountType = accountType,
            AccountClass = accountClass,
            ParentId = parentId,
            VatDefault = vatDefault,
            DatevAuto = datevAuto,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
