using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record GenerateDatevExportCommand : IRequest<Guid>
{
    public required Guid FiscalPeriodId { get; init; }
    public DatevExportType ExportType { get; init; } = DatevExportType.Buchungsstapel;
}

public class GenerateDatevExportCommandValidator : AbstractValidator<GenerateDatevExportCommand>
{
    public GenerateDatevExportCommandValidator()
    {
        RuleFor(x => x.FiscalPeriodId).NotEmpty();
    }
}

[RequirePermission("accounting.export")]
public class GenerateDatevExportCommandHandler : IRequestHandler<GenerateDatevExportCommand, Guid>
{
    private readonly IDatevExportService _exportService;
    private readonly ICurrentUser _currentUser;
    private readonly IAccountingHubNotifier _notifier;

    public GenerateDatevExportCommandHandler(
        IDatevExportService exportService, ICurrentUser currentUser,
        IAccountingHubNotifier notifier)
    {
        _exportService = exportService;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<Guid> Handle(
        GenerateDatevExportCommand request, CancellationToken cancellationToken)
    {
        var export = await _exportService.GenerateExportAsync(
            _currentUser.EntityId,
            request.FiscalPeriodId,
            request.ExportType,
            _currentUser.UserId,
            cancellationToken);

        if (export.Status == DatevExportStatus.Ready)
        {
            await _notifier.NotifyDatevExportReadyAsync(
                _currentUser.EntityId, export.Id, cancellationToken);
        }

        return export.Id;
    }
}
