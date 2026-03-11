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
            BookingSuggestion = bookingSuggestion is null ? null : new BookingSuggestionDto
            {
                Id = bookingSuggestion.Id,
                DebitAccountId = bookingSuggestion.DebitAccountId,
                CreditAccountId = bookingSuggestion.CreditAccountId,
                Amount = bookingSuggestion.Amount,
                VatCode = bookingSuggestion.VatCode,
                VatAmount = bookingSuggestion.VatAmount,
                Description = bookingSuggestion.Description,
                Confidence = bookingSuggestion.Confidence,
                Status = bookingSuggestion.Status,
                AiReasoning = bookingSuggestion.AiReasoning,
            },
        };
    }
}
