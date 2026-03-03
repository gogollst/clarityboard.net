using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Domain.Entities.Hr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.admin")]
public record ListDeletionRequestsQuery : IRequest<PagedResult<DeletionRequestDto>>
{
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record DeletionRequestDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeFullName { get; init; } = string.Empty;
    public Guid RequestedBy { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime ScheduledDeletionAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? BlockReason { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public class ListDeletionRequestsQueryHandler : IRequestHandler<ListDeletionRequestsQuery, PagedResult<DeletionRequestDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListDeletionRequestsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<DeletionRequestDto>> Handle(
        ListDeletionRequestsQuery request, CancellationToken cancellationToken)
    {
        // Scope to entity — only deletion requests for employees in the current entity
        // Use a correlated subquery to avoid materializing all employee IDs into memory
        var query = _db.DeletionRequests
            .Where(r => _db.Employees
                .Any(e => e.Id == r.EmployeeId && e.EntityId == _currentUser.EntityId))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<DeletionRequestStatus>(request.Status, ignoreCase: true, out var statusEnum))
        {
            query = query.Where(r => r.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rawItems = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                r.RequestedBy,
                r.RequestedAt,
                r.ScheduledDeletionAt,
                r.Status,
                r.BlockReason,
                r.CompletedAt,
            })
            .ToListAsync(cancellationToken);

        var employeeIds = rawItems.Select(r => r.EmployeeId).Distinct().ToList();
        var employeeNames = await _db.Employees
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}", cancellationToken);

        var items = rawItems.Select(r => new DeletionRequestDto
        {
            Id                  = r.Id,
            EmployeeId          = r.EmployeeId,
            EmployeeFullName    = employeeNames.TryGetValue(r.EmployeeId, out var name) ? name : string.Empty,
            RequestedBy         = r.RequestedBy,
            RequestedAt         = r.RequestedAt,
            ScheduledDeletionAt = r.ScheduledDeletionAt,
            Status              = r.Status.ToString(),
            BlockReason         = r.BlockReason,
            CompletedAt         = r.CompletedAt,
        }).ToList();

        return new PagedResult<DeletionRequestDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
