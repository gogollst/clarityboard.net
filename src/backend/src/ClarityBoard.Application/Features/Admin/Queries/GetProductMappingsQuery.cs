using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Admin.Queries;

public record GetProductMappingsQuery(Guid EntityId) : IRequest<IReadOnlyList<ProductMappingDto>>;

public record ProductMappingDto
{
    public Guid Id { get; init; }
    public string ProductNamePattern { get; init; } = default!;
    public string ProductCategory { get; init; } = default!;
    public Guid? RevenueAccountId { get; init; }
    public string? RevenueAccountNumber { get; init; }
    public string? RevenueAccountName { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class GetProductMappingsQueryHandler : IRequestHandler<GetProductMappingsQuery, IReadOnlyList<ProductMappingDto>>
{
    private readonly IAppDbContext _db;

    public GetProductMappingsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductMappingDto>> Handle(GetProductMappingsQuery request, CancellationToken ct)
    {
        return await _db.ProductCategoryMappings
            .Where(m => m.EntityId == request.EntityId)
            .Join(
                _db.Accounts.Where(a => a.EntityId == request.EntityId),
                m => m.RevenueAccountId,
                a => a.Id,
                (m, a) => new { Mapping = m, Account = a })
            .Select(x => new ProductMappingDto
            {
                Id = x.Mapping.Id,
                ProductNamePattern = x.Mapping.ProductNamePattern,
                ProductCategory = x.Mapping.ProductCategory,
                RevenueAccountId = x.Mapping.RevenueAccountId,
                RevenueAccountNumber = x.Account.AccountNumber,
                RevenueAccountName = x.Account.Name,
                IsActive = x.Mapping.IsActive,
                CreatedAt = x.Mapping.CreatedAt,
            })
            .Union(
                _db.ProductCategoryMappings
                    .Where(m => m.EntityId == request.EntityId && m.RevenueAccountId == null)
                    .Select(m => new ProductMappingDto
                    {
                        Id = m.Id,
                        ProductNamePattern = m.ProductNamePattern,
                        ProductCategory = m.ProductCategory,
                        RevenueAccountId = null,
                        RevenueAccountNumber = null,
                        RevenueAccountName = null,
                        IsActive = m.IsActive,
                        CreatedAt = m.CreatedAt,
                    })
            )
            .OrderBy(m => m.ProductCategory)
            .ThenBy(m => m.ProductNamePattern)
            .ToListAsync(ct);
    }
}
