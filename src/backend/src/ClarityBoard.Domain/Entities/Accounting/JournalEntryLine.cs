namespace ClarityBoard.Domain.Entities.Accounting;

public class JournalEntryLine
{
    public Guid Id { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public short LineNumber { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public decimal ExchangeRate { get; private set; } = 1.0m;
    public decimal BaseAmount { get; private set; }
    public string? VatCode { get; private set; }
    public decimal VatAmount { get; private set; }
    public string? CostCenter { get; private set; }    // legacy string field
    public Guid? CostCenterId { get; private set; }    // FK to accounting.cost_centers
    public Guid? HrEmployeeId { get; private set; }    // FK to hr.employees
    public Guid? HrTravelExpenseId { get; private set; } // FK to hr.travel_expense_reports
    public string? TaxCode { get; private set; }
    public string? Description { get; private set; }

    private JournalEntryLine() { } // EF Core

    public static JournalEntryLine CreateDebit(
        short lineNumber, Guid accountId, decimal amount,
        string? vatCode = null, decimal vatAmount = 0,
        string? costCenter = null, string? description = null,
        string currency = "EUR", decimal exchangeRate = 1.0m,
        Guid? costCenterId = null, Guid? hrEmployeeId = null,
        Guid? hrTravelExpenseId = null, string? taxCode = null)
    {
        return new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            LineNumber = lineNumber,
            AccountId = accountId,
            DebitAmount = amount,
            CreditAmount = 0,
            Currency = currency,
            ExchangeRate = exchangeRate,
            BaseAmount = amount * exchangeRate,
            VatCode = vatCode,
            VatAmount = vatAmount,
            CostCenter = costCenter,
            CostCenterId = costCenterId,
            HrEmployeeId = hrEmployeeId,
            HrTravelExpenseId = hrTravelExpenseId,
            TaxCode = taxCode,
            Description = description,
        };
    }

    public static JournalEntryLine CreateCredit(
        short lineNumber, Guid accountId, decimal amount,
        string? vatCode = null, decimal vatAmount = 0,
        string? costCenter = null, string? description = null,
        string currency = "EUR", decimal exchangeRate = 1.0m,
        Guid? costCenterId = null, Guid? hrEmployeeId = null,
        Guid? hrTravelExpenseId = null, string? taxCode = null)
    {
        return new JournalEntryLine
        {
            Id = Guid.NewGuid(),
            LineNumber = lineNumber,
            AccountId = accountId,
            DebitAmount = 0,
            CreditAmount = amount,
            Currency = currency,
            ExchangeRate = exchangeRate,
            BaseAmount = amount * exchangeRate,
            VatCode = vatCode,
            VatAmount = vatAmount,
            CostCenter = costCenter,
            CostCenterId = costCenterId,
            HrEmployeeId = hrEmployeeId,
            HrTravelExpenseId = hrTravelExpenseId,
            TaxCode = taxCode,
            Description = description,
        };
    }
}
