namespace ClarityBoard.Domain.Services;

public record VatResult(
    decimal Rate,
    string VatCode,
    string BuSchluessel,
    string InputVatAccount,
    string OutputVatAccount,
    string Description);

public class VatDeterminationService
{
    // Standard German VAT rates
    private const decimal StandardRate = 19.0m;
    private const decimal ReducedRate = 7.0m;
    private const decimal ZeroRate = 0.0m;

    // DATEV BU-Schlüssel (Booking Keys)
    private static class BuKeys
    {
        public const string Standard19 = "3"; // Umsatzsteuer 19%
        public const string Reduced7 = "2"; // Umsatzsteuer 7%
        public const string TaxFree = "0"; // steuerfrei
        public const string IntraEuDelivery = "11"; // ig. Lieferung § 4 Nr. 1b
        public const string IntraEuAcquisition19 = "91"; // ig. Erwerb 19%
        public const string IntraEuAcquisition7 = "92"; // ig. Erwerb 7%
        public const string ReverseCharge = "94"; // Reverse Charge § 13b
        public const string Export = "10"; // Ausfuhrlieferung
        public const string InputVat19 = "9"; // Vorsteuer 19%
        public const string InputVat7 = "8"; // Vorsteuer 7%
    }

    // SKR03 VAT accounts
    private static class VatAccounts
    {
        public const string OutputVat19 = "1776"; // Umsatzsteuer 19%
        public const string OutputVat7 = "1756"; // Umsatzsteuer 7%
        public const string InputVat19 = "1510"; // Vorsteuer 19%
        public const string InputVat7 = "1511"; // Vorsteuer 7%
        public const string ImportVat = "1518"; // Einfuhrumsatzsteuer
        public const string VatNotDue = "1770"; // USt nicht fällig
    }

    /// <summary>
    /// Determines VAT treatment for a sale/revenue transaction.
    /// </summary>
    public VatResult DetermineSaleVat(
        string customerCountryCode,
        bool customerHasVatId,
        string vatRateType = "standard")
    {
        // Export to non-EU country (Drittland)
        if (!IsEuCountry(customerCountryCode))
        {
            return new VatResult(
                ZeroRate, "UV0", BuKeys.Export,
                "", "",
                $"Tax-free export to {customerCountryCode} § 4 Nr. 1a UStG");
        }

        // Intra-EU delivery with valid VAT ID
        if (customerCountryCode != "DE" && customerHasVatId)
        {
            return new VatResult(
                ZeroRate, "UVEU", BuKeys.IntraEuDelivery,
                "", "",
                $"Tax-free intra-EU delivery to {customerCountryCode} § 4 Nr. 1b UStG");
        }

        // Intra-EU to private person or no VAT ID -> domestic rate applies
        if (customerCountryCode != "DE" && !customerHasVatId)
        {
            // Simplified: apply domestic rate. Full implementation would check OSS thresholds.
        }

        // Domestic sale
        return vatRateType switch
        {
            "reduced" => new VatResult(
                ReducedRate, "UV7", BuKeys.Reduced7,
                "", VatAccounts.OutputVat7,
                "Domestic sale 7% § 12 Abs. 2 UStG"),
            _ => new VatResult(
                StandardRate, "UV19", BuKeys.Standard19,
                "", VatAccounts.OutputVat19,
                "Domestic sale 19% § 12 Abs. 1 UStG"),
        };
    }

    /// <summary>
    /// Determines VAT treatment for a purchase/expense transaction.
    /// </summary>
    public VatResult DeterminePurchaseVat(
        string supplierCountryCode,
        bool supplierHasVatId,
        string vatRateType = "standard",
        bool isService = false)
    {
        // Import from non-EU (Drittland)
        if (!IsEuCountry(supplierCountryCode))
        {
            if (isService)
            {
                // Reverse charge for services from third countries § 13b
                return new VatResult(
                    StandardRate, "UVRC", BuKeys.ReverseCharge,
                    VatAccounts.InputVat19, VatAccounts.OutputVat19,
                    $"Reverse charge service from {supplierCountryCode} § 13b UStG");
            }

            return new VatResult(
                StandardRate, "UVIMP", "",
                VatAccounts.ImportVat, "",
                $"Import from {supplierCountryCode} - EUSt applies");
        }

        // Intra-EU acquisition
        if (supplierCountryCode != "DE" && supplierHasVatId)
        {
            var rate = vatRateType == "reduced" ? ReducedRate : StandardRate;
            var buKey = vatRateType == "reduced" ? BuKeys.IntraEuAcquisition7 : BuKeys.IntraEuAcquisition19;
            var inputAccount = vatRateType == "reduced" ? VatAccounts.InputVat7 : VatAccounts.InputVat19;
            var outputAccount = vatRateType == "reduced" ? VatAccounts.OutputVat7 : VatAccounts.OutputVat19;

            return new VatResult(
                rate, "UVEU_ACQ", buKey,
                inputAccount, outputAccount,
                $"Intra-EU acquisition from {supplierCountryCode} {rate}%");
        }

        // Domestic purchase
        return vatRateType switch
        {
            "reduced" => new VatResult(
                ReducedRate, "UV7", BuKeys.InputVat7,
                VatAccounts.InputVat7, "",
                "Domestic purchase 7%"),
            "zero" => new VatResult(
                ZeroRate, "UV0", BuKeys.TaxFree,
                "", "",
                "Tax-exempt purchase"),
            _ => new VatResult(
                StandardRate, "UV19", BuKeys.InputVat19,
                VatAccounts.InputVat19, "",
                "Domestic purchase 19%"),
        };
    }

    /// <summary>
    /// Calculates VAT amount from gross or net amount.
    /// </summary>
    public static decimal CalculateVatFromNet(decimal netAmount, decimal vatRate)
        => Math.Round(netAmount * vatRate / 100m, 2, MidpointRounding.AwayFromZero);

    public static decimal CalculateVatFromGross(decimal grossAmount, decimal vatRate)
        => Math.Round(grossAmount - grossAmount / (1m + vatRate / 100m), 2, MidpointRounding.AwayFromZero);

    public static decimal CalculateNetFromGross(decimal grossAmount, decimal vatRate)
        => Math.Round(grossAmount / (1m + vatRate / 100m), 2, MidpointRounding.AwayFromZero);

    private static bool IsEuCountry(string countryCode)
    {
        return EuCountries.Contains(countryCode.ToUpperInvariant());
    }

    private static readonly HashSet<string> EuCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR",
        "DE", "GR", "HU", "IE", "IT", "LV", "LT", "LU", "MT", "NL",
        "PL", "PT", "RO", "SK", "SI", "ES", "SE"
    };
}
