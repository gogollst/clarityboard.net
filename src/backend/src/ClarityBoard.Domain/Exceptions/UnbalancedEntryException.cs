namespace ClarityBoard.Domain.Exceptions;

public class UnbalancedEntryException : DomainException
{
    public decimal DebitTotal { get; }
    public decimal CreditTotal { get; }

    public UnbalancedEntryException(decimal debitTotal, decimal creditTotal)
        : base($"Journal entry is unbalanced. Debits: {debitTotal:F2}, Credits: {creditTotal:F2}", "UNBALANCED_ENTRY")
    {
        DebitTotal = debitTotal;
        CreditTotal = creditTotal;
    }
}
