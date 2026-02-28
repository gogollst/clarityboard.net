using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.CashFlow.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.CashFlow.Services;

public sealed class CashFlowForecastService : ICashFlowForecastService
{
    private readonly IAppDbContext _db;

    // Confidence weights for certainty levels
    private const decimal ConfirmedWeight = 1.00m;
    private const decimal ProbableWeight = 0.75m;
    private const decimal PossibleWeight = 0.50m;

    private const int ForecastWeeks = 13;

    public CashFlowForecastService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<CashFlowForecastDto> GenerateForecastAsync(
        Guid entityId, DateOnly startDate, CancellationToken ct = default)
    {
        var endDate = startDate.AddDays(ForecastWeeks * 7 - 1);

        // Load all entries within the forecast period for this entity
        var entries = await _db.CashFlowEntries
            .Where(e => e.EntityId == entityId
                && e.EntryDate >= startDate
                && e.EntryDate <= endDate)
            .ToListAsync(ct);

        // Calculate opening balance from all entries prior to startDate
        var openingBalance = await _db.CashFlowEntries
            .Where(e => e.EntityId == entityId
                && e.EntryDate < startDate
                && e.Certainty == "confirmed")
            .SumAsync(e => e.BaseAmount, ct);

        var weeks = new List<WeeklyForecastDto>();
        var cumulativeBalance = openingBalance;

        for (short week = 1; week <= ForecastWeeks; week++)
        {
            var weekStart = startDate.AddDays((week - 1) * 7);
            var weekEnd = weekStart.AddDays(6);

            var weekEntries = entries
                .Where(e => e.EntryDate >= weekStart && e.EntryDate <= weekEnd)
                .ToList();

            // Inflows by certainty level
            var confirmedInflow = weekEntries
                .Where(e => e.BaseAmount > 0 && e.Certainty == "confirmed")
                .Sum(e => e.BaseAmount);
            var probableInflow = weekEntries
                .Where(e => e.BaseAmount > 0 && e.Certainty == "probable")
                .Sum(e => e.BaseAmount);
            var possibleInflow = weekEntries
                .Where(e => e.BaseAmount > 0 && e.Certainty == "possible")
                .Sum(e => e.BaseAmount);

            // Outflows by certainty level (outflows are negative, so take absolute)
            var confirmedOutflow = Math.Abs(weekEntries
                .Where(e => e.BaseAmount < 0 && e.Certainty == "confirmed")
                .Sum(e => e.BaseAmount));
            var probableOutflow = Math.Abs(weekEntries
                .Where(e => e.BaseAmount < 0 && e.Certainty == "probable")
                .Sum(e => e.BaseAmount));
            var possibleOutflow = Math.Abs(weekEntries
                .Where(e => e.BaseAmount < 0 && e.Certainty == "possible")
                .Sum(e => e.BaseAmount));

            // Weighted net flow using confidence weights
            var weightedInflow = confirmedInflow * ConfirmedWeight
                + probableInflow * ProbableWeight
                + possibleInflow * PossibleWeight;
            var weightedOutflow = confirmedOutflow * ConfirmedWeight
                + probableOutflow * ProbableWeight
                + possibleOutflow * PossibleWeight;
            var weightedNetFlow = weightedInflow - weightedOutflow;

            cumulativeBalance += weightedNetFlow;

            // Confidence bounds: low = only confirmed, high = everything at 100%
            var lowNetFlow = confirmedInflow - confirmedOutflow;
            var highNetFlow = (confirmedInflow + probableInflow + possibleInflow)
                - (confirmedOutflow + probableOutflow + possibleOutflow);

            var confidenceLow = openingBalance
                + weeks.Sum(w => w.ConfirmedInflow - w.ConfirmedOutflow)
                + lowNetFlow;
            var confidenceHigh = openingBalance
                + weeks.Sum(w => w.ConfirmedInflow + w.ProbableInflow + w.PossibleInflow
                    - w.ConfirmedOutflow - w.ProbableOutflow - w.PossibleOutflow)
                + highNetFlow;

            weeks.Add(new WeeklyForecastDto
            {
                WeekNumber = week,
                WeekStartDate = weekStart,
                ConfirmedInflow = confirmedInflow,
                ProbableInflow = probableInflow,
                PossibleInflow = possibleInflow,
                ConfirmedOutflow = confirmedOutflow,
                ProbableOutflow = probableOutflow,
                PossibleOutflow = possibleOutflow,
                WeightedNetFlow = weightedNetFlow,
                CumulativeBalance = cumulativeBalance,
                ConfidenceLow = confidenceLow,
                ConfidenceHigh = confidenceHigh,
            });
        }

        return new CashFlowForecastDto
        {
            EntityId = entityId,
            StartDate = startDate,
            EndDate = endDate,
            OpeningBalance = openingBalance,
            Weeks = weeks,
            CalculatedAt = DateTime.UtcNow,
        };
    }
}
