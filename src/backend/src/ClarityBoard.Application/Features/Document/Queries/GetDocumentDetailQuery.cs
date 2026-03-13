using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Document;
using ClarityBoard.Application.Features.Document.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Document.Queries;

public record GetDocumentDetailQuery(Guid EntityId, Guid DocumentId) : IRequest<DocumentDetailDto?>, IEntityScoped;

public class GetDocumentDetailQueryHandler : IRequestHandler<GetDocumentDetailQuery, DocumentDetailDto?>
{
    private readonly IAppDbContext _db;

    public GetDocumentDetailQueryHandler(IAppDbContext db) => _db = db;

    public async Task<DocumentDetailDto?> Handle(GetDocumentDetailQuery request, CancellationToken ct)
    {
        var document = await _db.Documents
            .Include(d => d.Fields)
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.EntityId == request.EntityId, ct);

        if (document is null)
            return null;

        var bookingSuggestion = await _db.BookingSuggestions
            .Where(bs => bs.DocumentId == document.Id)
            .OrderByDescending(bs => bs.CreatedAt)
            .FirstOrDefaultAsync(ct);

        // Resolve account names for booking suggestion
        BookingSuggestionDto? bookingSuggestionDto = null;
        if (bookingSuggestion is not null)
        {
            var debitAccount = await _db.Accounts
                .Where(a => a.Id == bookingSuggestion.DebitAccountId)
                .Select(a => new { a.AccountNumber, a.Name })
                .FirstOrDefaultAsync(ct);

            var creditAccount = await _db.Accounts
                .Where(a => a.Id == bookingSuggestion.CreditAccountId)
                .Select(a => new { a.AccountNumber, a.Name })
                .FirstOrDefaultAsync(ct);

            string? employeeName = null;
            if (bookingSuggestion.HrEmployeeId.HasValue)
            {
                employeeName = await _db.Employees
                    .Where(e => e.Id == bookingSuggestion.HrEmployeeId.Value)
                    .Select(e => e.FirstName + " " + e.LastName)
                    .FirstOrDefaultAsync(ct);
            }

            bookingSuggestionDto = new BookingSuggestionDto
            {
                Id = bookingSuggestion.Id,
                DebitAccountId = bookingSuggestion.DebitAccountId,
                DebitAccountNumber = debitAccount?.AccountNumber,
                DebitAccountName = debitAccount?.Name,
                CreditAccountId = bookingSuggestion.CreditAccountId,
                CreditAccountNumber = creditAccount?.AccountNumber,
                CreditAccountName = creditAccount?.Name,
                Amount = bookingSuggestion.Amount,
                VatCode = bookingSuggestion.VatCode,
                VatAmount = bookingSuggestion.VatAmount,
                Description = bookingSuggestion.Description,
                Confidence = bookingSuggestion.Confidence,
                Status = bookingSuggestion.Status,
                AiReasoning = bookingSuggestion.AiReasoning,
                HrEmployeeId = bookingSuggestion.HrEmployeeId,
                HrEmployeeName = employeeName,
                IsAutoBooked = bookingSuggestion.IsAutoBooked,
                RejectionReason = bookingSuggestion.RejectionReason,
            };
        }

        // Resolve business partner names
        string? businessPartnerName = null;
        if (document.BusinessPartnerId.HasValue)
        {
            businessPartnerName = await _db.BusinessPartners
                .Where(bp => bp.Id == document.BusinessPartnerId.Value)
                .Select(bp => bp.Name)
                .FirstOrDefaultAsync(ct);
        }

        string? suggestedBusinessPartnerName = null;
        if (document.SuggestedBusinessPartnerId.HasValue)
        {
            suggestedBusinessPartnerName = await _db.BusinessPartners
                .Where(bp => bp.Id == document.SuggestedBusinessPartnerId.Value)
                .Select(bp => bp.Name)
                .FirstOrDefaultAsync(ct);
        }

        return new DocumentDetailDto
        {
            Id = document.Id,
            EntityId = document.EntityId,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            DocumentType = document.DocumentType,
            Status = document.Status,
            VendorName = document.VendorName,
            InvoiceNumber = document.InvoiceNumber,
            InvoiceDate = document.InvoiceDate,
            TotalAmount = document.TotalAmount,
            Currency = document.Currency,
            Confidence = document.Confidence,
            BookedJournalEntryId = document.BookedJournalEntryId,
            BusinessPartnerId = document.BusinessPartnerId,
            BusinessPartnerName = businessPartnerName,
            SuggestedBusinessPartnerId = document.SuggestedBusinessPartnerId,
            SuggestedBusinessPartnerName = suggestedBusinessPartnerName,
            CreatedAt = document.CreatedAt,
            ProcessedAt = document.ProcessedAt,
            ReviewReasons = DocumentExtractedDataReader.ReadReviewReasons(document.ExtractedData),
            Fields = document.Fields.Select(f => new DocumentFieldDto
            {
                Id = f.Id,
                FieldName = f.FieldName,
                FieldValue = f.FieldValue,
                Confidence = f.Confidence,
                IsVerified = f.IsVerified,
                CorrectedValue = f.CorrectedValue,
            }).ToList(),
            BookingSuggestion = bookingSuggestionDto,
        };
    }
}
