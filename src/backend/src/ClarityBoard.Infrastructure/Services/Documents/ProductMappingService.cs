using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Infrastructure.Services.Documents;

public class ProductMappingService
{
    private readonly IAppDbContext _db;

    public ProductMappingService(IAppDbContext db) => _db = db;

    /// <summary>
    /// Matches a product description against configured patterns and returns the category + optional revenue account.
    /// </summary>
    public async Task<ProductCategoryMatch?> MatchAsync(Guid entityId, string productDescription, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(productDescription))
            return null;

        var mappings = await _db.ProductCategoryMappings
            .Where(m => m.EntityId == entityId && m.IsActive)
            .ToListAsync(ct);

        var descriptionLower = productDescription.ToLowerInvariant();

        foreach (var mapping in mappings)
        {
            var pattern = mapping.ProductNamePattern.ToLowerInvariant();
            if (descriptionLower.Contains(pattern, StringComparison.Ordinal))
            {
                return new ProductCategoryMatch(
                    mapping.ProductCategory,
                    mapping.RevenueAccountId);
            }
        }

        return null;
    }
}

public record ProductCategoryMatch(string ProductCategory, Guid? RevenueAccountId);
