using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record BatchTranslateAccountNamesCommand : IRequest<BatchTranslateResult>;

public record BatchTranslateResult(int TranslatedCount, int SkippedCount, List<string> Errors);

[RequirePermission("admin.manage")]
public class BatchTranslateAccountNamesCommandHandler
    : IRequestHandler<BatchTranslateAccountNamesCommand, BatchTranslateResult>
{
    private readonly IAppDbContext _db;
    private readonly ITranslationService _translationService;
    private readonly ILogger<BatchTranslateAccountNamesCommandHandler> _logger;

    private static readonly string[] AllLanguages = ["de", "en", "ru"];

    public BatchTranslateAccountNamesCommandHandler(
        IAppDbContext db,
        ITranslationService translationService,
        ILogger<BatchTranslateAccountNamesCommandHandler> logger)
    {
        _db = db;
        _translationService = translationService;
        _logger = logger;
    }

    public async Task<BatchTranslateResult> Handle(
        BatchTranslateAccountNamesCommand request, CancellationToken cancellationToken)
    {
        var accounts = await _db.Accounts
            .Where(a => a.NameEn == null || a.NameRu == null)
            .ToListAsync(cancellationToken);

        var translated = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var account in accounts)
        {
            try
            {
                var sourceLang = "de";
                var sourceText = account.NameDe ?? account.Name;

                var targetLangs = AllLanguages
                    .Where(l => !l.Equals(sourceLang, StringComparison.OrdinalIgnoreCase));

                var translations = await _translationService.TranslateAsync(
                    sourceText, sourceLang, targetLangs, cancellationToken);

                if (translations.Count == 0)
                {
                    skipped++;
                    continue;
                }

                translations[sourceLang] = sourceText;
                account.SetTranslatedNames(
                    translations.GetValueOrDefault("de"),
                    translations.GetValueOrDefault("en"),
                    translations.GetValueOrDefault("ru"));

                translated++;

                // Save in batches of 50 to avoid long transactions
                if (translated % 50 == 0)
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Batch translated {Count} accounts so far", translated);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{account.AccountNumber} ({account.Name}): {ex.Message}");
                _logger.LogWarning(ex, "Failed to translate account {Id}", account.Id);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Batch translation complete: {Translated} translated, {Skipped} skipped, {Errors} errors",
            translated, skipped, errors.Count);

        return new BatchTranslateResult(translated, skipped, errors);
    }
}
