using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record JournalEntryListDto(
    Guid Id,
    long EntryNumber,
    DateOnly EntryDate,
    string Description,
    string Status,
    string? SourceType,
    decimal TotalAmount,
    int LineCount,
    DateTime CreatedAt);

public record GetJournalEntriesQuery(
    Guid EntityId,
    short? Year = null,
    short? Month = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 50) : IRequest<PagedResult<JournalEntryListDto>>, IEntityScoped;

public class GetJournalEntriesQueryHandler : IRequestHandler<GetJournalEntriesQuery, PagedResult<JournalEntryListDto>>
{
    private readonly IAppDbContext _db;

    public GetJournalEntriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<JournalEntryListDto>> Handle(GetJournalEntriesQuery request, CancellationToken ct)
    {
        var query = _db.JournalEntries
            .Where(j => j.EntityId == request.EntityId);

        if (request.Year.HasValue)
            query = query.Where(j => j.EntryDate.Year == request.Year.Value);
        if (request.Month.HasValue)
            query = query.Where(j => j.EntryDate.Month == request.Month.Value);
        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(j => j.Status == request.Status);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(j => j.EntryNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(j => new JournalEntryListDto(
                j.Id,
                j.EntryNumber,
                j.EntryDate,
                j.Description,
                j.Status,
                j.SourceType,
                j.Lines.Sum(l => l.DebitAmount),
                j.Lines.Count(),
                j.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<JournalEntryListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
