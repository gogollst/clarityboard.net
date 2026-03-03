using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record ListLeaveTypesQuery(Guid EntityId) : IRequest<List<LeaveTypeDto>>;

public record LeaveTypeDto
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool RequiresApproval { get; init; }
    public bool IsDeductedFromBalance { get; init; }
    public int? MaxDaysPerYear { get; init; }
    public string Color { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

// Fix 5: add minimal validator
public class ListLeaveTypesQueryValidator : AbstractValidator<ListLeaveTypesQuery>
{
    public ListLeaveTypesQueryValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
    }
}

public class ListLeaveTypesQueryHandler : IRequestHandler<ListLeaveTypesQuery, List<LeaveTypeDto>>
{
    private readonly IAppDbContext _db;

    public ListLeaveTypesQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<LeaveTypeDto>> Handle(ListLeaveTypesQuery request, CancellationToken cancellationToken)
    {
        return await _db.LeaveTypes
            .Where(lt => lt.EntityId == request.EntityId)
            .OrderBy(lt => lt.Name)
            .Select(lt => new LeaveTypeDto
            {
                Id                    = lt.Id,
                EntityId              = lt.EntityId,
                Name                  = lt.Name,
                Code                  = lt.Code,
                RequiresApproval      = lt.RequiresApproval,
                IsDeductedFromBalance = lt.IsDeductedFromBalance,
                MaxDaysPerYear        = lt.MaxDaysPerYear,
                Color                 = lt.Color,
                IsActive              = lt.IsActive,
            })
            .ToListAsync(cancellationToken);
    }
}
