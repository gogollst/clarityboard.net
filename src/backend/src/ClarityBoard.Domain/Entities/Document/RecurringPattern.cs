namespace ClarityBoard.Domain.Entities.Document;

public class RecurringPattern
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string VendorName { get; private set; } = default!;
    public string? VendorPattern { get; private set; } // Regex or fuzzy match pattern
    public Guid DebitAccountId { get; private set; }
    public Guid CreditAccountId { get; private set; }
    public string? VatCode { get; private set; }
    public string? CostCenter { get; private set; }
    public int MatchCount { get; private set; } // How many times this pattern has been used
    public decimal Confidence { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastMatchedAt { get; private set; }
    public Guid? BusinessPartnerId { get; private set; }
    public Guid? HrEmployeeId { get; private set; }
    public int AutoBookThreshold { get; private set; } = 3;
    public bool AutoBookEnabled { get; private set; }

    private RecurringPattern() { }

    public static RecurringPattern Create(
        Guid entityId, string vendorName,
        Guid debitAccountId, Guid creditAccountId,
        string? vatCode, string? costCenter,
        decimal confidence, string? vendorPattern = null)
    {
        return new RecurringPattern
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            VendorName = vendorName,
            VendorPattern = vendorPattern,
            DebitAccountId = debitAccountId,
            CreditAccountId = creditAccountId,
            VatCode = vatCode,
            CostCenter = costCenter,
            MatchCount = 1,
            Confidence = confidence,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void IncrementMatch()
    {
        MatchCount++;
        LastMatchedAt = DateTime.UtcNow;
        // Increase confidence with more matches, cap at 0.99
        Confidence = Math.Min(0.99m, Confidence + 0.01m);

        if (MatchCount >= AutoBookThreshold)
            AutoBookEnabled = true;
    }

    public void UpdateAccounts(Guid debitAccountId, Guid creditAccountId, string? vatCode)
    {
        DebitAccountId = debitAccountId;
        CreditAccountId = creditAccountId;
        VatCode = vatCode;
    }

    public void SetEmployee(Guid? hrEmployeeId)
    {
        HrEmployeeId = hrEmployeeId;
    }

    public void AssignBusinessPartner(Guid? businessPartnerId)
    {
        BusinessPartnerId = businessPartnerId;
    }
}
