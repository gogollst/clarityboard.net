using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Entity.Commands;

[RequirePermission("entity.edit")]
public record SetEntityActiveCommand : IRequest
{
    public required Guid Id { get; init; }
    public required bool IsActive { get; init; }
}

public class SetEntityActiveCommandValidator : AbstractValidator<SetEntityActiveCommand>
{
    public SetEntityActiveCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class SetEntityActiveCommandHandler : IRequestHandler<SetEntityActiveCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public SetEntityActiveCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task Handle(SetEntityActiveCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LegalEntities
            .FirstOrDefaultAsync(le => le.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Entity with id '{request.Id}' was not found.");

        entity.SetActive(request.IsActive);

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: entity.Id,
            action: request.IsActive ? "reactivate" : "deactivate",
            tableName: "legal_entities",
            recordId: entity.Id.ToString(),
            oldValues: null,
            newValues: $"{{\"isActive\":{request.IsActive.ToString().ToLower()}}}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);
    }
}

