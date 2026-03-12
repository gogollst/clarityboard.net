using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ClarityBoard.Application.Features.Accounting.Commands;

[RequirePermission("accounting.plan")]
public record UpdateAccountCommand : IRequest
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? VatDefault { get; init; }
    public string? CostCenterDefault { get; init; }
    public string? BwaLine { get; init; }
    public bool IsAutoPosting { get; init; }
    public string? SourceLanguage { get; init; }
}

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.VatDefault).MaximumLength(10).When(x => x.VatDefault != null);
        RuleFor(x => x.BwaLine).MaximumLength(10).When(x => x.BwaLine != null);
    }
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITranslationService _translationService;

    private static readonly string[] AllLanguages = ["de", "en", "ru"];

    public UpdateAccountCommandHandler(IAppDbContext db, ICurrentUser currentUser, ITranslationService translationService)
    {
        _db = db;
        _currentUser = currentUser;
        _translationService = translationService;
    }

    public async Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.Id
                && a.EntityId == _currentUser.EntityId, cancellationToken)
            ?? throw new InvalidOperationException("Account not found.");

        account.Update(
            request.Name,
            request.VatDefault,
            request.CostCenterDefault,
            request.BwaLine,
            request.IsAutoPosting);

        // Re-translate if name changed
        var sourceLang = request.SourceLanguage ?? "de";
        var targetLangs = AllLanguages.Where(l => !l.Equals(sourceLang, StringComparison.OrdinalIgnoreCase));
        var translations = await _translationService.TranslateAsync(request.Name, sourceLang, targetLangs, cancellationToken);

        translations[sourceLang] = request.Name;
        account.SetTranslatedNames(
            translations.GetValueOrDefault("de"),
            translations.GetValueOrDefault("en"),
            translations.GetValueOrDefault("ru"));

        await _db.SaveChangesAsync(cancellationToken);
    }
}
