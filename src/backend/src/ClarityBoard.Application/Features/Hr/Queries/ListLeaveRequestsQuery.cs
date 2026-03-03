using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record ListLeaveRequestsQuery : IRequest<PagedResult<LeaveRequestDto>>
{
    public Guid? EmployeeId { get; init; }
    public string? Status { get; init; }
    public int? Year { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record LeaveRequestDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeFullName { get; init; } = string.Empty;
    public string LeaveTypeName { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal WorkingDays { get; init; }
    public bool HalfDay { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
}

public class ListLeaveRequestsQueryValidator : AbstractValidator<ListLeaveRequestsQuery>
{
    public ListLeaveRequestsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class ListLeaveRequestsQueryHandler : IRequestHandler<ListLeaveRequestsQuery, PagedResult<LeaveRequestDto>>
{
    private readonly IAppDbContext _db;

    public ListLeaveRequestsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<LeaveRequestDto>> Handle(ListLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.LeaveRequests.AsQueryable();

        if (request.EmployeeId.HasValue)
            query = query.Where(r => r.EmployeeId == request.EmployeeId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<LeaveRequestStatus>(request.Status, ignoreCase: true, out var statusEnum))
        {
            query = query.Where(r => r.Status == statusEnum);
        }

        if (request.Year.HasValue)
            query = query.Where(r => r.StartDate.Year == request.Year.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var rawItems = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                r.LeaveTypeId,
                r.StartDate,
                r.EndDate,
                r.WorkingDays,
                r.HalfDay,
                r.Status,
                r.Notes,
                r.RejectionReason,
                r.RequestedAt,
                r.ApprovedAt,
            })
            .ToListAsync(cancellationToken);

        // Resolve employee names
        var employeeIds = rawItems.Select(r => r.EmployeeId).Distinct().ToList();
        var employeeNames = await _db.Employees
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}", cancellationToken);

        // Resolve leave type names
        var leaveTypeIds = rawItems.Select(r => r.LeaveTypeId).Distinct().ToList();
        var leaveTypeNames = await _db.LeaveTypes
            .Where(lt => leaveTypeIds.Contains(lt.Id))
            .Select(lt => new { lt.Id, lt.Name })
            .ToDictionaryAsync(lt => lt.Id, lt => lt.Name, cancellationToken);

        var items = rawItems.Select(r => new LeaveRequestDto
        {
            Id               = r.Id,
            EmployeeId       = r.EmployeeId,
            EmployeeFullName = employeeNames.TryGetValue(r.EmployeeId, out var empName) ? empName : string.Empty,
            LeaveTypeName    = leaveTypeNames.TryGetValue(r.LeaveTypeId, out var ltName) ? ltName : string.Empty,
            StartDate        = r.StartDate,
            EndDate          = r.EndDate,
            WorkingDays      = r.WorkingDays,
            HalfDay          = r.HalfDay,
            Status           = r.Status.ToString(),
            Notes            = r.Notes,
            RejectionReason  = r.RejectionReason,
            RequestedAt      = r.RequestedAt,
            ApprovedAt       = r.ApprovedAt,
        }).ToList();

        return new PagedResult<LeaveRequestDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
