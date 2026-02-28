using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Budget.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Budget.Queries;

public record GetBudgetsQuery : IRequest<IReadOnlyList<BudgetListDto>>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public short? FiscalYear { get; init; }
    public string? Status { get; init; }
    public string? Department { get; init; }
}

public class GetBudgetsQueryHandler
    : IRequestHandler<GetBudgetsQuery, IReadOnlyList<BudgetListDto>>
{
    private readonly IAppDbContext _db;

    public GetBudgetsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<BudgetListDto>> Handle(
        GetBudgetsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Budgets
            .Where(b => b.EntityId == request.EntityId);

        if (request.FiscalYear.HasValue)
            query = query.Where(b => b.FiscalYear == request.FiscalYear.Value);
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(b => b.Status == request.Status);
        if (!string.IsNullOrEmpty(request.Department))
            query = query.Where(b => b.Department == request.Department);

        return await query
            .OrderByDescending(b => b.FiscalYear)
            .ThenByDescending(b => b.CreatedAt)
            .Select(b => new BudgetListDto
            {
                Id = b.Id,
                EntityId = b.EntityId,
                Name = b.Name,
                FiscalYear = b.FiscalYear,
                BudgetType = b.BudgetType,
                Status = b.Status,
                TotalAmount = b.TotalAmount,
                Department = b.Department,
                LineCount = b.Lines.Count,
                CreatedAt = b.CreatedAt,
                ApprovedAt = b.ApprovedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
