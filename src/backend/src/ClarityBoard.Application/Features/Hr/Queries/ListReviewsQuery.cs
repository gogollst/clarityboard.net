using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record ListReviewsQuery : IRequest<PagedResult<PerformanceReviewDto>>
{
    public Guid? EmployeeId { get; init; }
    public string? ReviewType { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record PerformanceReviewDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeFullName { get; init; } = string.Empty;
    public Guid ReviewerId { get; init; }
    public string ReviewerFullName { get; init; } = string.Empty;
    public DateOnly ReviewPeriodStart { get; init; }
    public DateOnly ReviewPeriodEnd { get; init; }
    public string ReviewType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int? OverallRating { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class ListReviewsQueryValidator : AbstractValidator<ListReviewsQuery>
{
    public ListReviewsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class ListReviewsQueryHandler : IRequestHandler<ListReviewsQuery, PagedResult<PerformanceReviewDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListReviewsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<PerformanceReviewDto>> Handle(
        ListReviewsQuery request, CancellationToken cancellationToken)
    {
        // Scope to current entity
        var entityEmployeeIds = await _db.Employees
            .Where(e => e.EntityId == _currentUser.EntityId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var query = _db.PerformanceReviews
            .Where(r => entityEmployeeIds.Contains(r.EmployeeId));

        if (request.EmployeeId.HasValue)
            query = query.Where(r => r.EmployeeId == request.EmployeeId.Value);

        if (!string.IsNullOrWhiteSpace(request.ReviewType)
            && Enum.TryParse<ReviewType>(request.ReviewType, ignoreCase: true, out var reviewTypeEnum))
        {
            query = query.Where(r => r.ReviewType == reviewTypeEnum);
        }

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<ReviewStatus>(request.Status, ignoreCase: true, out var statusEnum))
        {
            query = query.Where(r => r.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rawItems = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new
            {
                r.Id,
                r.EmployeeId,
                r.ReviewerId,
                r.ReviewPeriodStart,
                r.ReviewPeriodEnd,
                r.ReviewType,
                r.Status,
                r.OverallRating,
                r.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        // Batch-load employee names (EmployeeId values only — ReviewerId is a UserId, not an EmployeeId)
        var employeeIds = rawItems.Select(r => r.EmployeeId).Distinct().ToList();
        var employeeNames = await _db.Employees
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName })
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);

        // Reviewer names (via Users, because ReviewerId is a UserId)
        var reviewerUserIds = rawItems.Select(r => r.ReviewerId).Distinct().ToList();
        var reviewerNames = await _db.Users
            .Where(u => reviewerUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}", cancellationToken);

        var items = rawItems.Select(r => new PerformanceReviewDto
        {
            Id               = r.Id,
            EmployeeId       = r.EmployeeId,
            EmployeeFullName = employeeNames.TryGetValue(r.EmployeeId, out var eName) ? eName : string.Empty,
            ReviewerId       = r.ReviewerId,
            ReviewerFullName = reviewerNames.TryGetValue(r.ReviewerId, out var rName) ? rName : string.Empty,
            ReviewPeriodStart = r.ReviewPeriodStart,
            ReviewPeriodEnd   = r.ReviewPeriodEnd,
            ReviewType       = r.ReviewType.ToString(),
            Status           = r.Status.ToString(),
            OverallRating    = r.OverallRating,
            CreatedAt        = r.CreatedAt,
        }).ToList();

        return new PagedResult<PerformanceReviewDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
