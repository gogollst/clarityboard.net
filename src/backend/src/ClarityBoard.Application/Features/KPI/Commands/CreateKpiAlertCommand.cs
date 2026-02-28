using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.KPI;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Commands;

public record CreateKpiAlertCommand : IRequest<Guid>
{
    public required string KpiId { get; init; }
    public required string Name { get; init; }
    public required string Condition { get; init; }
    public decimal ThresholdValue { get; init; }
    public required string Severity { get; init; }
    public string? TargetRoles { get; init; }
    public string? Channels { get; init; }
}

public class CreateKpiAlertCommandValidator : AbstractValidator<CreateKpiAlertCommand>
{
    public CreateKpiAlertCommandValidator()
    {
        RuleFor(x => x.KpiId).NotEmpty().MaximumLength(100);
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

public class CreateKpiAlertCommandHandler : IRequestHandler<CreateKpiAlertCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateKpiAlertCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateKpiAlertCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        // Validate that KPI definition exists
        var kpiExists = await _db.KpiDefinitions
            .AnyAsync(d => d.Id == request.KpiId && d.IsActive, cancellationToken);

        if (!kpiExists)
            throw new InvalidOperationException($"KPI definition '{request.KpiId}' not found.");

        var alert = KpiAlert.Create(
            entityId,
            request.KpiId,
            request.Name,
            request.Condition,
            request.ThresholdValue,
            request.Severity,
            request.TargetRoles,
            request.Channels);

        _db.KpiAlerts.Add(alert);
        await _db.SaveChangesAsync(cancellationToken);

        return alert.Id;
    }
}
