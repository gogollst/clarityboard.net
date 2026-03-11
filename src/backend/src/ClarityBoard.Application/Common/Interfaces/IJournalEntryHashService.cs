namespace ClarityBoard.Application.Common.Interfaces;

public interface IJournalEntryHashService
{
    string ComputeHash(Guid entryId, Guid entityId, long entryNumber,
        DateOnly entryDate, string description,
        IEnumerable<(Guid AccountId, decimal DebitAmount, decimal CreditAmount)> lines,
        string previousHash);
}
