using System.Security.Cryptography;
using System.Text;
using ClarityBoard.Application.Common.Interfaces;

namespace ClarityBoard.Infrastructure.Services;

public class JournalEntryHashService : IJournalEntryHashService
{
    public string ComputeHash(
        Guid entryId, Guid entityId, long entryNumber,
        DateOnly entryDate, string description,
        IEnumerable<(Guid AccountId, decimal DebitAmount, decimal CreditAmount)> lines,
        string previousHash)
    {
        var linesStr = string.Join("|", lines.Select(l =>
            $"{l.AccountId}:{l.DebitAmount:F2}:{l.CreditAmount:F2}"));

        var input = $"{entryId}|{entityId}|{entryNumber}|{entryDate:yyyy-MM-dd}|{description}|{linesStr}|{previousHash}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
