using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Integration.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Integration.Queries;

public record GetMappingRulesQuery : IRequest<IReadOnlyList<MappingRuleDto>>
{
    public string? SourceType { get; init; }
}

public class GetMappingRulesQueryHandler
    : IRequestHandler<GetMappingRulesQuery, IReadOnlyList<MappingRuleDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMappingRulesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<MappingRuleDto>> Handle(
        GetMappingRulesQuery request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var query = _db.MappingRules
            .Where(r => r.EntityId == entityId);

        if (!string.IsNullOrWhiteSpace(request.SourceType))
            query = query.Where(r => r.SourceType == request.SourceType);

        var rules = await query
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .Select(r => new MappingRuleDto
            {
                Id = r.Id,
                EntityId = r.EntityId,
                SourceType = r.SourceType,
                EventType = r.EventType,
                FieldMapping = r.FieldMapping,
                DebitAccountId = r.DebitAccountId,
                CreditAccountId = r.CreditAccountId,
                VatCode = r.VatCode,
                CostCenter = r.CostCenter,
                Condition = r.Condition,
                Priority = r.Priority,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return rules;
    }
}
