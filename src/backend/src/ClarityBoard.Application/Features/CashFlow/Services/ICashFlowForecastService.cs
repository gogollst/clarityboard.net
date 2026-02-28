using ClarityBoard.Application.Features.CashFlow.DTOs;

namespace ClarityBoard.Application.Features.CashFlow.Services;

public interface ICashFlowForecastService
{
    /// <summary>
    /// Generates a 13-week rolling cash flow forecast projection for the given entity.
    /// </summary>
    Task<CashFlowForecastDto> GenerateForecastAsync(
        Guid entityId, DateOnly startDate, CancellationToken ct = default);
}
