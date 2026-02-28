using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.KPI;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Commands;

public record UpdateKpiAlertCommand : IRequest
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Condition { get; init; }
    public decimal ThresholdValue { get; init; }
    public required string Severity { get; init; }
    public string? TargetRoles { get; init; }
    public string? Channels { get; init; }
}

public class UpdateKpiAlertCommandValidator : AbstractValidator<UpdateKpiAlertCommand>
{
    public UpdateKpiAlertCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Condition)
            .NotEmpty()
            .Must(c => KpiAlert.ValidConditions.Contains(c))
            .WithMessage($"Condition must be one of: {string.Join(", ", KpiAlert.ValidConditions)}.");
        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(s => KpiAlert.ValidSeverities.Contains(s))
            .WithMessage($"Severity must be one of: {string.Join(", ", KpiAlert.ValidSeverities)}.");
    }
}

public class UpdateKpiAlertCommandHandler : IRequestHandler<UpdateKpiAlertCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateKpiAlertCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateKpiAlertCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var alert = await _db.KpiAlerts
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.EntityId == entityId, cancellationToken)
            ?? throw new InvalidOperationException($"Alert '{request.Id}' not found.");

        alert.Update(
            request.Name,
            request.Condition,
            request.ThresholdValue,
            request.Severity,
            request.TargetRoles,
            request.Channels);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
