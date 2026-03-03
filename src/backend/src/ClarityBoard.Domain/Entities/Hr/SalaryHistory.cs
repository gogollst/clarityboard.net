namespace ClarityBoard.Domain.Entities.Hr;

public class SalaryHistory
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public SalaryType SalaryType { get; private set; }
    public int GrossAmountCents { get; private set; }
    public string CurrencyCode { get; private set; } = "EUR";
    public int BonusAmountCents { get; private set; }
    public string BonusCurrencyCode { get; private set; } = "EUR";
    public int PaymentCycleMonths { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string ChangeReason { get; private set; } = string.Empty;

    private SalaryHistory() { }

    public static SalaryHistory Create(Guid employeeId, SalaryType type,
        int grossAmountCents, string currencyCode,
        int bonusAmountCents, string bonusCurrencyCode,
        int paymentCycleMonths, DateTime validFrom, Guid createdBy, string changeReason)
    {
        if (grossAmountCents < 0)
            throw new ArgumentException("Salary cannot be negative.");
        if (string.IsNullOrWhiteSpace(changeReason))
            throw new ArgumentException("Change reason is required.");
        return new SalaryHistory
        {
            Id                 = Guid.NewGuid(),
            EmployeeId         = employeeId,
            SalaryType         = type,
            GrossAmountCents   = grossAmountCents,
            CurrencyCode       = currencyCode,
            BonusAmountCents   = bonusAmountCents,
            BonusCurrencyCode  = bonusCurrencyCode,
            PaymentCycleMonths = paymentCycleMonths,
            ValidFrom          = validFrom,
            CreatedBy          = createdBy,
            ChangeReason       = changeReason,
        };
    }

    public void Close(DateTime validTo)
    {
        if (validTo <= ValidFrom)
            throw new ArgumentException("ValidTo must be after ValidFrom.", nameof(validTo));
        ValidTo = validTo;
    }
}
