using ClarityBoard.Domain.Entities.Entity;

namespace ClarityBoard.Application.Common.Interfaces;

public interface IDocumentClassifier
{
    Task<DocumentClassificationResult> ClassifyAsync(
        string ocrText,
        IReadOnlyList<LegalEntity> entities,
        CancellationToken ct);
}

public record DocumentClassificationResult(
    string Direction,        // "incoming" or "outgoing"
    decimal Confidence,      // 0.00 - 1.00
    int Score,               // Raw weighted score (-30 to +100)
    List<string> MatchedRules);
