namespace ClarityBoard.Domain.Entities.Accounting;

public class ProductCategoryMapping
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string ProductNamePattern { get; private set; } = default!; // keyword or regex
    public string ProductCategory { get; private set; } = default!; // SAAS_LICENSE, HOSTING, etc.
    public Guid? RevenueAccountId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private ProductCategoryMapping() { }

    public static ProductCategoryMapping Create(
        Guid entityId, string productNamePattern, string productCategory,
        Guid? revenueAccountId = null)
    {
        return new ProductCategoryMapping
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            ProductNamePattern = productNamePattern,
            ProductCategory = productCategory,
            RevenueAccountId = revenueAccountId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string productNamePattern, string productCategory, Guid? revenueAccountId, bool isActive)
    {
        ProductNamePattern = productNamePattern;
        ProductCategory = productCategory;
        RevenueAccountId = revenueAccountId;
        IsActive = isActive;
    }
}
