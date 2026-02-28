using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record JournalEntryDetailLineDto(
    Guid Id,
    short LineNumber,
    Guid AccountId,
    string AccountNumber,
    string AccountName,
    decimal DebitAmount,
    decimal CreditAmount,
    string Currency,
    decimal VatAmount,
    string? VatCode,
    string? CostCenter,
    string? Description);

public record JournalEntryDetailDto(
    Guid Id,
    long EntryNumber,
    DateOnly EntryDate,
    DateOnly PostingDate,
    string Description,
    string Status,
    string? SourceType,
    string? SourceRef,
    Guid FiscalPeriodId,
    bool IsReversal,
    Guid? ReversalOf,
    string Hash,
    DateTime CreatedAt,
    Guid CreatedBy,
    List<JournalEntryDetailLineDto> Lines);

public record GetJournalEntryDetailQuery(Guid EntityId, Guid JournalEntryId) : IRequest<JournalEntryDetailDto>, IEntityScoped;

public class GetJournalEntryDetailQueryHandler : IRequestHandler<GetJournalEntryDetailQuery, JournalEntryDetailDto>
{
    private readonly IAppDbContext _db;

    public GetJournalEntryDetailQueryHandler(IAppDbContext db) => _db = db;

    public async Task<JournalEntryDetailDto> Handle(GetJournalEntryDetailQuery request, CancellationToken ct)
    {
        var entry = await _db.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == request.JournalEntryId && j.EntityId == request.EntityId, ct)
            ?? throw new NotFoundException($"Journal entry '{request.JournalEntryId}' not found.");

        var accountIds = entry.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);

        var lines = entry.Lines
            .OrderBy(l => l.LineNumber)
            .Select(l =>
            {
                accounts.TryGetValue(l.AccountId, out var account);
                return new JournalEntryDetailLineDto(
                    l.Id, l.LineNumber, l.AccountId,
                    account?.AccountNumber ?? "", account?.Name ?? "",
                    l.DebitAmount, l.CreditAmount, l.Currency,
                    l.VatAmount, l.VatCode, l.CostCenter, l.Description);
            }).ToList();

        return new JournalEntryDetailDto(
            entry.Id, entry.EntryNumber, entry.EntryDate, entry.PostingDate,
            entry.Description, entry.Status, entry.SourceType, entry.SourceRef,
            entry.FiscalPeriodId, entry.IsReversal, entry.ReversalOf,
            entry.Hash, entry.CreatedAt, entry.CreatedBy, lines);
    }
}
