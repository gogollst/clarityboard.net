using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetLeaveBalanceQuery(Guid EmployeeId, int? Year = null) : IRequest<List<LeaveBalanceDto>>;

public record LeaveBalanceDto
{
    public Guid LeaveTypeId { get; init; }
    public string LeaveTypeName { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal EntitlementDays { get; init; }
    public decimal UsedDays { get; init; }
    public decimal PendingDays { get; init; }
    public decimal CarryOverDays { get; init; }
    public decimal RemainingDays { get; init; }
}

// Fix 5: add minimal validator
public class GetLeaveBalanceQueryValidator : AbstractValidator<GetLeaveBalanceQuery>
{
    public GetLeaveBalanceQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}

public class GetLeaveBalanceQueryHandler : IRequestHandler<GetLeaveBalanceQuery, List<LeaveBalanceDto>>
{
    private readonly IAppDbContext _db;

    public GetLeaveBalanceQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<LeaveBalanceDto>> Handle(GetLeaveBalanceQuery request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee is null)
            return [];

        var year = request.Year ?? DateTime.UtcNow.Year;

        // Get all active leave types for this entity
        var leaveTypes = await _db.LeaveTypes
            .Where(lt => lt.EntityId == employee.EntityId && lt.IsActive)
            .OrderBy(lt => lt.Name)
            .Select(lt => new { lt.Id, lt.Name })
            .ToListAsync(cancellationToken);

        // Get existing balances
        var balances = await _db.LeaveBalances
            .Where(b => b.EmployeeId == request.EmployeeId && b.Year == year)
            .ToListAsync(cancellationToken);

        var balanceMap = balances.ToDictionary(b => b.LeaveTypeId);

        return leaveTypes.Select(lt =>
        {
            if (balanceMap.TryGetValue(lt.Id, out var balance))
            {
                return new LeaveBalanceDto
                {
                    LeaveTypeId     = lt.Id,
                    LeaveTypeName   = lt.Name,
                    Year            = year,
                    EntitlementDays = balance.EntitlementDays,
                    UsedDays        = balance.UsedDays,
                    PendingDays     = balance.PendingDays,
                    CarryOverDays   = balance.CarryOverDays,
                    RemainingDays   = balance.RemainingDays,
                };
            }

            return new LeaveBalanceDto
            {
                LeaveTypeId     = lt.Id,
                LeaveTypeName   = lt.Name,
                Year            = year,
                EntitlementDays = 0,
                UsedDays        = 0,
                PendingDays     = 0,
                CarryOverDays   = 0,
                RemainingDays   = 0,
            };
        }).ToList();
    }
}
