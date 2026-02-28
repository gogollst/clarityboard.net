using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Integration.Commands;

public record UpdateMappingRuleCommand : IRequest
{
    public Guid Id { get; init; }
    public required string FieldMapping { get; init; }
    public int Priority { get; init; }
    public string? Condition { get; init; }
    public Guid? DebitAccountId { get; init; }
    public Guid? CreditAccountId { get; init; }
    public string? VatCode { get; init; }
    public string? CostCenter { get; init; }
    public bool IsActive { get; init; } = true;
}

public class UpdateMappingRuleCommandValidator : AbstractValidator<UpdateMappingRuleCommand>
{
    public UpdateMappingRuleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FieldMapping).NotEmpty();
    }
}

public class UpdateMappingRuleCommandHandler : IRequestHandler<UpdateMappingRuleCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateMappingRuleCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(
        UpdateMappingRuleCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var rule = await _db.MappingRules
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.EntityId == entityId, cancellationToken)
            ?? throw new InvalidOperationException($"Mapping rule '{request.Id}' not found.");

        rule.Update(
            request.FieldMapping,
            request.Priority,
            request.Condition,
            request.DebitAccountId,
            request.CreditAccountId,
            request.VatCode,
            request.CostCenter,
            request.IsActive);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
