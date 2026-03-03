namespace ClarityBoard.Domain.Entities.Hr;

public class TravelExpenseItem
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public ExpenseType ExpenseType { get; private set; }
    public DateOnly ExpenseDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int OriginalAmountCents { get; private set; }
    public string OriginalCurrencyCode { get; private set; } = string.Empty;
    public decimal ExchangeRate { get; private set; }
    public DateOnly ExchangeRateDate { get; private set; }
    public int AmountCents { get; private set; }  // in report currency (EUR)
    public string CurrencyCode { get; private set; } = "EUR";
    public Guid? ReceiptDocumentId { get; private set; }
    public decimal? VatRatePercent { get; private set; }
    public bool IsDeductible { get; private set; }

    private TravelExpenseItem() { }

    public static TravelExpenseItem Create(Guid reportId, ExpenseType type, DateOnly expenseDate,
        string description, int originalAmountCents, string originalCurrencyCode,
        decimal exchangeRate, DateOnly exchangeRateDate, bool isDeductible = true, decimal? vatRate = null)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required.", nameof(description));
        if (string.IsNullOrWhiteSpace(originalCurrencyCode)) throw new ArgumentException("Currency code is required.", nameof(originalCurrencyCode));
        if (originalAmountCents < 0) throw new ArgumentException("Amount cannot be negative.", nameof(originalAmountCents));
        if (exchangeRate <= 0) throw new ArgumentException("Exchange rate must be positive.", nameof(exchangeRate));
        var amountCents = (int)Math.Round(originalAmountCents * exchangeRate);
        return new TravelExpenseItem
        {
            Id                   = Guid.NewGuid(),
            ReportId             = reportId,
            ExpenseType          = type,
            ExpenseDate          = expenseDate,
            Description          = description,
            OriginalAmountCents  = originalAmountCents,
            OriginalCurrencyCode = originalCurrencyCode,
            ExchangeRate         = exchangeRate,
            ExchangeRateDate     = exchangeRateDate,
            AmountCents          = amountCents,
            CurrencyCode         = "EUR",
            IsDeductible         = isDeductible,
            VatRatePercent       = vatRate,
        };
    }

    public void SetReceiptDocument(Guid documentId) => ReceiptDocumentId = documentId;
}
