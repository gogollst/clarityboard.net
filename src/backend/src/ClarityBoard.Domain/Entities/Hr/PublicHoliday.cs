namespace ClarityBoard.Domain.Entities.Hr;

public class PublicHoliday
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = "DE";

    private PublicHoliday() { }

    public static PublicHoliday Create(Guid entityId, DateOnly date, string name, string countryCode = "DE")
    => new()
    {
        Id          = Guid.NewGuid(),
        EntityId    = entityId,
        Date        = date,
        Name        = name,
        CountryCode = countryCode,
    };
}
