using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Integration;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Integration.Commands;

public record CreateMappingRuleCommand : IRequest<Guid>
{
    public required string SourceType { get; init; }
    public required string EventType { get; init; }
    public required string FieldMapping { get; init; }
    public int Priority { get; init; }
    public string? Condition { get; init; }
    public Guid? DebitAccountId { get; init; }
    public Guid? CreditAccountId { get; init; }
    public string? VatCode { get; init; }
    public string? CostCenter { get; init; }
    public bool IsActive { get; init; } = true;
}

public class CreateMappingRuleCommandValidator : AbstractValidator<CreateMappingRuleCommand>
{
    public CreateMappingRuleCommandValidator()
    {
        RuleFor(x => x.SourceType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FieldMapping).NotEmpty();
    }
}

public class CreateMappingRuleCommandHandler : IRequestHandler<CreateMappingRuleCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateMappingRuleCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateMappingRuleCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var rule = MappingRule.Create(
            entityId,
            request.SourceType,
            request.EventType,
            request.FieldMapping,
            request.Priority,
            request.Condition,
            request.DebitAccountId,
            request.CreditAccountId,
            request.VatCode,
            request.CostCenter);

        _db.MappingRules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);

        return rule.Id;
    }
}
