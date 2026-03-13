namespace ClarityBoard.Application.Common.Helpers;

public static class AmountReconciler
{
    private const decimal Tolerance = 0.02m;

    public record Result(
        decimal? GrossAmount,
        decimal? NetAmount,
        decimal? TaxAmount,
        bool PlausibilityMismatch,
        string? MismatchDetail);

    public static Result Reconcile(decimal? grossAmount, decimal? netAmount, decimal? taxAmount)
    {
        // If we have net + tax but no gross, calculate gross
        if (grossAmount is null && netAmount.HasValue && taxAmount.HasValue)
            grossAmount = netAmount.Value + taxAmount.Value;

        // If we have gross + tax but no net, calculate net
        if (netAmount is null && grossAmount.HasValue && taxAmount.HasValue)
            netAmount = grossAmount.Value - taxAmount.Value;

        // If we have gross + net but no tax, calculate tax
        if (taxAmount is null && grossAmount.HasValue && netAmount.HasValue)
            taxAmount = grossAmount.Value - netAmount.Value;

        // Plausibility: gross should equal net + tax (within rounding tolerance)
        bool mismatch = false;
        string? detail = null;

        if (grossAmount.HasValue && netAmount.HasValue && taxAmount.HasValue)
        {
            var expected = netAmount.Value + taxAmount.Value;
            if (Math.Abs(grossAmount.Value - expected) > Tolerance)
            {
                mismatch = true;
                detail = $"Brutto {grossAmount:F2} ≠ Netto {netAmount:F2} + USt {taxAmount:F2} (= {expected:F2})";
            }
        }

        return new Result(grossAmount, netAmount, taxAmount, mismatch, detail);
    }
}
