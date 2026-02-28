namespace ClarityBoard.Domain.Entities.CashFlow;

public class CashFlowForecast
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public DateOnly ForecastDate { get; private set; }
    public short WeekNumber { get; private set; }
    public decimal ProjectedInflow { get; private set; }
    public decimal ProjectedOutflow { get; private set; }
    public decimal ProjectedBalance { get; private set; }
    public decimal ConfidenceLow { get; private set; }
    public decimal ConfidenceHigh { get; private set; }
    public string? Assumptions { get; private set; } // JSON
    public DateTime CalculatedAt { get; private set; }

    private CashFlowForecast() { }

    public static CashFlowForecast Create(
        Guid entityId, DateOnly forecastDate, short weekNumber,
        decimal projectedInflow, decimal projectedOutflow,
        decimal projectedBalance, decimal confidenceLow,
        decimal confidenceHigh, string? assumptions = null)
    {
        return new CashFlowForecast
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            ForecastDate = forecastDate,
            WeekNumber = weekNumber,
            ProjectedInflow = projectedInflow,
            ProjectedOutflow = projectedOutflow,
            ProjectedBalance = projectedBalance,
            ConfidenceLow = confidenceLow,
            ConfidenceHigh = confidenceHigh,
            Assumptions = assumptions,
            CalculatedAt = DateTime.UtcNow,
        };
    }
}
