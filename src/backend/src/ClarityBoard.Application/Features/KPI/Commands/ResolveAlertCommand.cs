using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.KPI.Commands;

public record ResolveAlertCommand : IRequest
{
    public Guid Id { get; init; }
}

public class ResolveAlertCommandValidator : AbstractValidator<ResolveAlertCommand>
{
    public ResolveAlertCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ResolveAlertCommandHandler : IRequestHandler<ResolveAlertCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ResolveAlertCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(
        ResolveAlertCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var alertEvent = await _db.KpiAlertEvents
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.EntityId == entityId, cancellationToken)
            ?? throw new InvalidOperationException($"Alert event '{request.Id}' not found.");

        alertEvent.Resolve();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
