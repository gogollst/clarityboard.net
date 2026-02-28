using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Commands;

public record AcknowledgeAlertCommand : IRequest
{
    public Guid Id { get; init; }
}

public class AcknowledgeAlertCommandValidator : AbstractValidator<AcknowledgeAlertCommand>
{
    public AcknowledgeAlertCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class AcknowledgeAlertCommandHandler : IRequestHandler<AcknowledgeAlertCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AcknowledgeAlertCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(
        AcknowledgeAlertCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var alertEvent = await _db.KpiAlertEvents
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.EntityId == entityId, cancellationToken)
            ?? throw new InvalidOperationException($"Alert event '{request.Id}' not found.");

        alertEvent.Acknowledge(_currentUser.UserId);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
