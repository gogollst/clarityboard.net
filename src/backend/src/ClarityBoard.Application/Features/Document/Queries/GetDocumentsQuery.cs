using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.Document.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Document.Queries;

public record GetDocumentsQuery : IRequest<PagedResult<DocumentListDto>>, IEntityScoped
{
    public Guid EntityId { get; init; }
    public string? Status { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, PagedResult<DocumentListDto>>
{
    private readonly IAppDbContext _db;

    public GetDocumentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<DocumentListDto>> Handle(GetDocumentsQuery request, CancellationToken ct)
    {
        var query = _db.Documents.Where(d => d.EntityId == request.EntityId);

        if (!string.IsNullOrEmpty(request.Status))
            query = query.Where(d => d.Status == request.Status);

        if (request.DateFrom.HasValue)
            query = query.Where(d => d.InvoiceDate >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(d => d.InvoiceDate <= request.DateTo.Value);

        if (!string.IsNullOrEmpty(request.Search))
        {
            var term = request.Search.ToLower();
            query = query.Where(d =>
                (d.VendorName != null && d.VendorName.ToLower().Contains(term)) ||
                (d.InvoiceNumber != null && d.InvoiceNumber.ToLower().Contains(term)) ||
                d.FileName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DocumentListDto
            {
                Id = d.Id,
                FileName = d.FileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                DocumentType = d.DocumentType,
                Status = d.Status,
                VendorName = d.VendorName,
                InvoiceNumber = d.InvoiceNumber,
                InvoiceDate = d.InvoiceDate,
                TotalAmount = d.TotalAmount,
                NetAmount = d.NetAmount,
                TaxAmount = d.TaxAmount,
                Currency = d.Currency,
                Confidence = d.Confidence,
                CreatedAt = d.CreatedAt,
                ProcessedAt = d.ProcessedAt,
            })
            .ToListAsync(ct);

        return new PagedResult<DocumentListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
