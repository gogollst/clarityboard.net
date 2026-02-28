namespace ClarityBoard.Domain.Exceptions;

public class ClosedPeriodException : DomainException
{
    public short Year { get; }
    public short Month { get; }
    public string PeriodStatus { get; }

    public ClosedPeriodException(short year, short month, string periodStatus)
        : base($"Cannot post to period {year}/{month:D2} with status '{periodStatus}'.", "CLOSED_PERIOD")
    {
        Year = year;
        Month = month;
        PeriodStatus = periodStatus;
    }
}
