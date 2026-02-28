namespace ClarityBoard.Domain.Entities.KPI;

public class KpiDefinition
{
    public string Id { get; private set; } = default!; // e.g. "financial.gross_margin"
    public string Domain { get; private set; } = default!; // financial, sales, marketing, hr, general
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string Formula { get; private set; } = default!;
    public string Unit { get; private set; } = default!; // percentage, currency, ratio, count, days
    public string Direction { get; private set; } = default!; // higher_better, lower_better, target
    public string Dependencies { get; private set; } = "[]"; // JSON array of KPI IDs
    public string? DefaultTarget { get; private set; } // JSON
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }
    public string? Category { get; private set; } // Sub-category like "profitability", "liquidity"
    public string? CalculationClass { get; private set; } // Fully qualified class name that performs calculation

    private KpiDefinition() { } // EF Core

    public static KpiDefinition Create(
        string id, string domain, string name, string formula, string unit, string direction,
        string? category = null, string? calculationClass = null, int displayOrder = 0,
        string? description = null, string? dependencies = null)
    {
        return new KpiDefinition
        {
            Id = id,
            Domain = domain,
            Name = name,
            Formula = formula,
            Unit = unit,
            Direction = direction,
            Category = category,
            CalculationClass = calculationClass,
            DisplayOrder = displayOrder,
            Description = description,
            Dependencies = dependencies ?? "[]",
        };
    }
}
