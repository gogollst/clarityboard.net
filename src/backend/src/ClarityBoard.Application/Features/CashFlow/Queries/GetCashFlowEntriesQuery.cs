using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.CashFlow.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.CashFlow.Queries;

public record GetCashFlowEntriesQuery : IRequest<PagedResult<CashFlowEntryDto>>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public string? Category { get; init; }
    public string? Certainty { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public class GetCashFlowEntriesQueryHandler
    : IRequestHandler<GetCashFlowEntriesQuery, PagedResult<CashFlowEntryDto>>
{
    private readonly IAppDbContext _db;

    public GetCashFlowEntriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<CashFlowEntryDto>> Handle(
        GetCashFlowEntriesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.CashFlowEntries
            .Where(e => e.EntityId == request.EntityId);

        if (request.From.HasValue)
            query = query.Where(e => e.EntryDate >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(e => e.EntryDate <= request.To.Value);
        if (!string.IsNullOrEmpty(request.Category))
            query = query.Where(e => e.Category == request.Category);
        if (!string.IsNullOrEmpty(request.Certainty))
            query = query.Where(e => e.Certainty == request.Certainty);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new CashFlowEntryDto
            {
                Id = e.Id,
                EntityId = e.EntityId,
                EntryDate = e.EntryDate,
                Category = e.Category,
                Subcategory = e.Subcategory,
                Amount = e.Amount,
                Currency = e.Currency,
                BaseAmount = e.BaseAmount,
                SourceType = e.SourceType,
                Description = e.Description,
                IsRecurring = e.IsRecurring,
                Certainty = e.Certainty,
                CreatedAt = e.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CashFlowEntryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
