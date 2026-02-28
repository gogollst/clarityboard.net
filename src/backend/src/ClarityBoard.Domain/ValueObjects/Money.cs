namespace ClarityBoard.Domain.ValueObjects;

public record Money(decimal Amount, string Currency = "EUR")
{
    public static Money Zero(string currency = "EUR") => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");

        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {Currency} and {other.Currency}");

        return this with { Amount = Amount - other.Amount };
    }

    public Money Multiply(decimal factor) => this with { Amount = Amount * factor };

    public Money Negate() => this with { Amount = -Amount };

    public override string ToString() => $"{Amount:N2} {Currency}";
}
