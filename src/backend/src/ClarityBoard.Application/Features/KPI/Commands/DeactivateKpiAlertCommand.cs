using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Commands;

public record DeactivateKpiAlertCommand : IRequest
{
    public Guid Id { get; init; }
}

public class DeactivateKpiAlertCommandValidator : AbstractValidator<DeactivateKpiAlertCommand>
{
    public DeactivateKpiAlertCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class DeactivateKpiAlertCommandHandler : IRequestHandler<DeactivateKpiAlertCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeactivateKpiAlertCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(
        DeactivateKpiAlertCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var alert = await _db.KpiAlerts
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.EntityId == entityId, cancellationToken)
            ?? throw new InvalidOperationException($"Alert '{request.Id}' not found.");

        alert.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
