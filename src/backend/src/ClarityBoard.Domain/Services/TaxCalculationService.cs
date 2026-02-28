namespace ClarityBoard.Domain.Services;

public record TaxCalculationResult(
    decimal KoerperschaftsteuerBase,
    decimal Koerperschaftsteuer,
    decimal Solidaritaetszuschlag,
    decimal GewerbesteuerMessbetrag,
    decimal Gewerbesteuer,
    decimal TotalTaxExpense,
    decimal EffectiveTaxRate);

public record TaxConfig(
    decimal Hebesatz = 400m,
    decimal KstRate = 15.0m,
    decimal SoliRate = 5.5m,
    decimal GewStMesszahl = 3.5m,
    decimal GewStFreibetrag = 0m,
    decimal KstFreibetrag = 0m);

public class TaxCalculationService
{
    /// <summary>
    /// Calculates German corporate taxes (KSt + Soli + GewSt) for a GmbH/AG.
    /// </summary>
    public TaxCalculationResult CalculateCorporateTax(
        decimal preTaxIncome,
        TaxConfig? config = null)
    {
        config ??= new TaxConfig();

        // Step 1: Körperschaftsteuer (Corporate Income Tax)
        var kstBase = Math.Max(0, preTaxIncome - config.KstFreibetrag);
        var kst = Math.Round(kstBase * config.KstRate / 100m, 2);

        // Step 2: Solidaritätszuschlag (5.5% of KSt)
        var soli = Math.Round(kst * config.SoliRate / 100m, 2);

        // Step 3: Gewerbesteuer (Trade Tax)
        // GewSt = Gewerbeertrag * Steuermesszahl * Hebesatz / 100
        var gewerbeertrag = Math.Max(0, preTaxIncome - config.GewStFreibetrag);
        var messbetrag = Math.Round(gewerbeertrag * config.GewStMesszahl / 100m, 2);
        var gewst = Math.Round(messbetrag * config.Hebesatz / 100m, 2);

        var totalTax = kst + soli + gewst;
        var effectiveRate = preTaxIncome > 0
            ? Math.Round(totalTax / preTaxIncome * 100m, 2)
            : 0m;

        return new TaxCalculationResult(
            KoerperschaftsteuerBase: kstBase,
            Koerperschaftsteuer: kst,
            Solidaritaetszuschlag: soli,
            GewerbesteuerMessbetrag: messbetrag,
            Gewerbesteuer: gewst,
            TotalTaxExpense: totalTax,
            EffectiveTaxRate: effectiveRate);
    }

    /// <summary>
    /// Calculates tax shield from interest expense deductibility.
    /// </summary>
    public decimal CalculateTaxShield(decimal interestExpense, decimal effectiveTaxRate)
    {
        return Math.Round(interestExpense * effectiveTaxRate / 100m, 2);
    }

    /// <summary>
    /// Calculates quarterly advance tax payments based on prior year.
    /// </summary>
    public decimal CalculateQuarterlyAdvance(TaxCalculationResult priorYear)
    {
        // Quarterly advance = (KSt + Soli) / 4
        // GewSt quarterly = GewSt / 4
        return Math.Round((priorYear.Koerperschaftsteuer + priorYear.Solidaritaetszuschlag + priorYear.Gewerbesteuer) / 4m, 2);
    }
}
