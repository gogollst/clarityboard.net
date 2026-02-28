using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.CashFlow.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.CashFlow.Queries;

public record GetCashFlowOverviewQuery : IRequest<CashFlowOverviewDto>
{
    public Guid EntityId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

public class GetCashFlowOverviewQueryHandler : IRequestHandler<GetCashFlowOverviewQuery, CashFlowOverviewDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCashFlowOverviewQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CashFlowOverviewDto> Handle(
        GetCashFlowOverviewQuery request, CancellationToken cancellationToken)
    {
        var entityId = request.EntityId != Guid.Empty ? request.EntityId : _currentUser.EntityId;
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = request.From ?? new DateOnly(now.Year, now.Month, 1);
        var to = request.To ?? now;

        var entries = await _db.CashFlowEntries
            .Where(e => e.EntityId == entityId
                && e.EntryDate >= from
                && e.EntryDate <= to)
            .ToListAsync(cancellationToken);

        // Operating
        var operatingInflow = entries
            .Where(e => e.Category == "operating_inflow")
            .Sum(e => e.BaseAmount);
        var operatingOutflow = entries
            .Where(e => e.Category == "operating_outflow")
            .Sum(e => e.BaseAmount);

        // Investing
        var investingEntries = entries.Where(e => e.Category == "investing").ToList();
        var investingInflow = investingEntries.Where(e => e.BaseAmount > 0).Sum(e => e.BaseAmount);
        var investingOutflow = Math.Abs(investingEntries.Where(e => e.BaseAmount < 0).Sum(e => e.BaseAmount));

        // Financing
        var financingEntries = entries.Where(e => e.Category == "financing").ToList();
        var financingInflow = financingEntries.Where(e => e.BaseAmount > 0).Sum(e => e.BaseAmount);
        var financingOutflow = Math.Abs(financingEntries.Where(e => e.BaseAmount < 0).Sum(e => e.BaseAmount));

        var operatingNet = operatingInflow + operatingOutflow; // outflow is already negative in BaseAmount
        var investingNet = investingInflow - investingOutflow;
        var financingNet = financingInflow - financingOutflow;

        return new CashFlowOverviewDto
        {
            OperatingInflow = operatingInflow,
            OperatingOutflow = Math.Abs(operatingOutflow),
            OperatingNet = operatingNet,
            InvestingInflow = investingInflow,
            InvestingOutflow = investingOutflow,
            InvestingNet = investingNet,
            FinancingInflow = financingInflow,
            FinancingOutflow = financingOutflow,
            FinancingNet = financingNet,
            TotalNet = operatingNet + investingNet + financingNet,
            PeriodStart = from,
            PeriodEnd = to,
        };
    }
}
