using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using ClarityBoard.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Document.Commands;

public record ApproveBookingCommand : IRequest<Guid>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public Guid DocumentId { get; init; }
    public Guid UserId { get; init; }
    public Guid? HrEmployeeId { get; init; }
    public Guid? TargetEntityId { get; init; }
}

public class ApproveBookingCommandHandler : IRequestHandler<ApproveBookingCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IAccountingRepository _accountingRepo;
    private readonly IBookingPatternLearner _patternLearner;

    public ApproveBookingCommandHandler(IAppDbContext db, IAccountingRepository accountingRepo, IBookingPatternLearner patternLearner)
    {
        _db = db;
        _accountingRepo = accountingRepo;
        _patternLearner = patternLearner;
    }

    public async Task<Guid> Handle(ApproveBookingCommand request, CancellationToken ct)
    {
        // Load document
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == request.EntityId, ct)
            ?? throw new InvalidOperationException($"Document {request.DocumentId} not found.");

        // Load booking suggestion
        var suggestion = await _db.BookingSuggestions
            .Where(bs => bs.DocumentId == request.DocumentId && bs.Status == "suggested")
            .OrderByDescending(bs => bs.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No pending booking suggestion for document {request.DocumentId}.");

        // Determine target entity: user override > AI suggestion > uploaded entity
        var targetEntityId = request.TargetEntityId
                             ?? suggestion.SuggestedEntityId
                             ?? request.EntityId;

        // Find or create fiscal period for the invoice date
        var invoiceDate = document.InvoiceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fiscalPeriod = await _db.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == targetEntityId
                && fp.StartDate <= invoiceDate
                && fp.EndDate >= invoiceDate, ct)
            ?? throw new InvalidOperationException(
                $"No fiscal period found for date {invoiceDate}. Please create the fiscal period first.");

        // Get next entry number
        var entryNumber = await _accountingRepo.GetNextEntryNumberAsync(targetEntityId, ct);

        // Check for deferred revenue (PRA) — outgoing invoices with planned revenue schedule entries
        var isOutgoing = document.DocumentDirection == "outgoing";
        var deferredEntries = isOutgoing
            ? await _db.RevenueScheduleEntries
                .Where(e => e.DocumentId == document.Id && e.Status == "planned")
                .ToListAsync(ct)
            : [];

        var hasDeferredRevenue = deferredEntries.Count > 0;

        // Create JournalEntry from BookingSuggestion
        var description = isOutgoing
            ? suggestion.Description ?? $"AR: {document.VendorName} {document.InvoiceNumber}"
            : suggestion.Description ?? $"Invoice: {document.VendorName} {document.InvoiceNumber}";

        var journalEntry = JournalEntry.Create(
            entityId: targetEntityId,
            entryNumber: entryNumber,
            entryDate: invoiceDate,
            description: description,
            fiscalPeriodId: fiscalPeriod.Id,
            createdBy: request.UserId,
            sourceType: "ai-suggestion",
            sourceRef: suggestion.Id.ToString(),
            documentId: document.Id);

        if (hasDeferredRevenue)
        {
            // Multi-line journal entry for outgoing invoice with PRA:
            // Soll 1400 (Forderungen) = Bruttobetrag
            // Haben revenue account = Sofort-Erlös (1. Monat)
            // Haben 3900 (PRA) = abgegrenzter Erlös
            // Haben 3806 (USt) = volle MwSt (fällig bei Rechnungsstellung)
            short lineNo = 1;

            // Debit: Full gross amount on receivables account (1400)
            journalEntry.AddLine(JournalEntryLine.CreateDebit(
                lineNumber: lineNo++,
                accountId: suggestion.DebitAccountId, // 1400 Forderungen
                amount: suggestion.Amount,
                vatCode: suggestion.VatCode,
                vatAmount: suggestion.VatAmount ?? 0,
                description: description));

            // Credit: VAT (full amount due at invoice time per UStG)
            if (suggestion.VatAmount.HasValue && suggestion.VatAmount.Value > 0)
            {
                var vatAccountId = await ResolveAccountIdAsync(targetEntityId, "3806", ct);
                if (vatAccountId.HasValue)
                {
                    journalEntry.AddLine(JournalEntryLine.CreateCredit(
                        lineNumber: lineNo++,
                        accountId: vatAccountId.Value,
                        amount: suggestion.VatAmount.Value,
                        description: $"USt {document.InvoiceNumber}"));
                }
            }

            // Credit: Immediate revenue (gross - VAT - deferred)
            var deferredTotal = deferredEntries.Sum(e => e.Amount);
            var netAmount = suggestion.Amount - (suggestion.VatAmount ?? 0);
            var immediateRevenue = netAmount - deferredTotal;

            if (immediateRevenue > 0)
            {
                journalEntry.AddLine(JournalEntryLine.CreateCredit(
                    lineNumber: lineNo++,
                    accountId: suggestion.CreditAccountId, // Revenue account (4110/4400)
                    amount: immediateRevenue,
                    description: $"Erlös {document.InvoiceNumber} (Sofort)"));
            }

            // Credit: Deferred revenue on PRA account (3900)
            if (deferredTotal > 0)
            {
                var praAccountId = await ResolveAccountIdAsync(targetEntityId, "3900", ct);
                if (praAccountId.HasValue)
                {
                    journalEntry.AddLine(JournalEntryLine.CreateCredit(
                        lineNumber: lineNo++,
                        accountId: praAccountId.Value,
                        amount: deferredTotal,
                        description: $"PRA {document.InvoiceNumber}"));
                }
            }
        }
        else
        {
            var vatAmount = suggestion.VatAmount ?? 0;
            var netAmount = suggestion.Amount - vatAmount;
            short lineNo = 1;

            if (vatAmount > 0)
            {
                if (isOutgoing)
                {
                    // Outgoing invoice (Ausgangsrechnung):
                    // Soll: Forderungen (1400) = Bruttobetrag
                    // Haben: Erlöskonto = Nettobetrag
                    // Haben: Umsatzsteuer (3806/3801) = MwSt
                    journalEntry.AddLine(JournalEntryLine.CreateDebit(
                        lineNumber: lineNo++,
                        accountId: suggestion.DebitAccountId,
                        amount: suggestion.Amount, // Gross on receivables
                        vatCode: suggestion.VatCode,
                        vatAmount: vatAmount,
                        description: suggestion.Description,
                        hrEmployeeId: request.HrEmployeeId));

                    journalEntry.AddLine(JournalEntryLine.CreateCredit(
                        lineNumber: lineNo++,
                        accountId: suggestion.CreditAccountId,
                        amount: netAmount, // NET on revenue account
                        description: suggestion.Description,
                        hrEmployeeId: request.HrEmployeeId));

                    // Credit: Umsatzsteuer
                    var ustAccountNum = IsReducedRate(vatAmount, netAmount) ? "3801" : "3806";
                    var ustAccountId = await ResolveAccountIdAsync(targetEntityId, ustAccountNum, ct);
                    if (ustAccountId.HasValue)
                    {
                        journalEntry.AddLine(JournalEntryLine.CreateCredit(
                            lineNumber: lineNo++,
                            accountId: ustAccountId.Value,
                            amount: vatAmount,
                            description: $"USt {document.InvoiceNumber}"));
                    }
                }
                else
                {
                    // Incoming invoice (Eingangsrechnung):
                    // Soll: Aufwandskonto = Nettobetrag
                    // Soll: Vorsteuer (1406/1401) = MwSt
                    // Haben: Verbindlichkeiten (1600) = Bruttobetrag
                    journalEntry.AddLine(JournalEntryLine.CreateDebit(
                        lineNumber: lineNo++,
                        accountId: suggestion.DebitAccountId,
                        amount: netAmount, // NET on expense account
                        vatCode: suggestion.VatCode,
                        vatAmount: vatAmount,
                        description: suggestion.Description,
                        hrEmployeeId: request.HrEmployeeId));

                    // Debit: Vorsteuer
                    var vstAccountNum = IsReducedRate(vatAmount, netAmount) ? "1401" : "1406";
                    var vstAccountId = await ResolveAccountIdAsync(targetEntityId, vstAccountNum, ct);
                    if (vstAccountId.HasValue)
                    {
                        journalEntry.AddLine(JournalEntryLine.CreateDebit(
                            lineNumber: lineNo++,
                            accountId: vstAccountId.Value,
                            amount: vatAmount,
                            description: $"VSt {document.InvoiceNumber}"));
                    }

                    journalEntry.AddLine(JournalEntryLine.CreateCredit(
                        lineNumber: lineNo++,
                        accountId: suggestion.CreditAccountId,
                        amount: suggestion.Amount, // Gross on payables
                        vatCode: suggestion.VatCode,
                        vatAmount: vatAmount,
                        description: suggestion.Description,
                        hrEmployeeId: request.HrEmployeeId));
                }
            }
            else
            {
                // No VAT — simple two-line entry
                journalEntry.AddLine(JournalEntryLine.CreateDebit(
                    lineNumber: lineNo++,
                    accountId: suggestion.DebitAccountId,
                    amount: suggestion.Amount,
                    vatCode: suggestion.VatCode,
                    vatAmount: 0,
                    description: suggestion.Description,
                    hrEmployeeId: request.HrEmployeeId));

                journalEntry.AddLine(JournalEntryLine.CreateCredit(
                    lineNumber: lineNo++,
                    accountId: suggestion.CreditAccountId,
                    amount: suggestion.Amount,
                    vatCode: suggestion.VatCode,
                    vatAmount: 0,
                    description: suggestion.Description,
                    hrEmployeeId: request.HrEmployeeId));
            }
        }

        // Save journal entry
        await _accountingRepo.AddJournalEntryAsync(journalEntry, ct);

        // Accept the booking suggestion
        suggestion.Accept(request.UserId, request.HrEmployeeId);

        // Mark document as booked
        document.MarkBooked(journalEntry.Id);

        await _db.SaveChangesAsync(ct);

        // Learn from the decision for recurring patterns
        await _patternLearner.LearnFromDecisionAsync(
            targetEntityId, document.VendorName, document.BusinessPartnerId,
            suggestion.DebitAccountId, suggestion.CreditAccountId, suggestion.VatCode,
            request.HrEmployeeId, ct, document.DocumentDirection);

        await _db.SaveChangesAsync(ct);

        return journalEntry.Id;
    }

    /// <summary>
    /// Determines if the VAT rate is reduced (7%) rather than standard (19%).
    /// </summary>
    private static bool IsReducedRate(decimal vatAmount, decimal netAmount)
    {
        if (netAmount <= 0) return false;
        var rate = vatAmount / netAmount;
        return rate < 0.12m; // 7% = 0.07, 19% = 0.19 — threshold at 12%
    }

    private async Task<Guid?> ResolveAccountIdAsync(Guid entityId, string accountNumber, CancellationToken ct)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.EntityId == entityId && a.AccountNumber == accountNumber, ct);
        return account?.Id;
    }
}
