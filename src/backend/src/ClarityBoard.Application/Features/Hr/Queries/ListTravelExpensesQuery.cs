using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record ListTravelExpensesQuery : IRequest<PagedResult<TravelExpenseReportDto>>
{
    public Guid? EntityId { get; init; }
    public Guid? EmployeeId { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record TravelExpenseReportDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeFullName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateOnly TripStartDate { get; init; }
    public DateOnly TripEndDate { get; init; }
    public string Destination { get; init; } = string.Empty;
    public string BusinessPurpose { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int TotalAmountCents { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public class ListTravelExpensesQueryValidator : AbstractValidator<ListTravelExpensesQuery>
{
    public ListTravelExpensesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class ListTravelExpensesQueryHandler : IRequestHandler<ListTravelExpensesQuery, PagedResult<TravelExpenseReportDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListTravelExpensesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<TravelExpenseReportDto>> Handle(
        ListTravelExpensesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.TravelExpenseReports.AsQueryable();

        // Always scope to an entity: use the explicitly requested one or default to the current user's entity
        var entityId = request.EntityId ?? _currentUser.EntityId;
        var entityEmployeeIds = await _db.Employees
            .Where(e => e.EntityId == entityId)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
        query = query.Where(r => entityEmployeeIds.Contains(r.EmployeeId));

        if (request.EmployeeId.HasValue)
            query = query.Where(r => r.EmployeeId == request.EmployeeId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<TravelExpenseStatus>(request.Status, ignoreCase: true, out var statusEnum))
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
                r.Title,
                r.TripStartDate,
                r.TripEndDate,
                r.Destination,
                r.BusinessPurpose,
                r.Status,
                r.TotalAmountCents,
                r.CurrencyCode,
                r.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var employeeIds = rawItems.Select(r => r.EmployeeId).Distinct().ToList();
        var employeeNames = await _db.Employees
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .ToDictionaryAsync(e => e.Id, e => $"{e.FirstName} {e.LastName}", cancellationToken);

        var items = rawItems.Select(r => new TravelExpenseReportDto
        {
            Id               = r.Id,
            EmployeeId       = r.EmployeeId,
            EmployeeFullName = employeeNames.TryGetValue(r.EmployeeId, out var name) ? name : string.Empty,
            Title            = r.Title,
            TripStartDate    = r.TripStartDate,
            TripEndDate      = r.TripEndDate,
            Destination      = r.Destination,
            BusinessPurpose  = r.BusinessPurpose,
            Status           = r.Status.ToString(),
            TotalAmountCents = r.TotalAmountCents,
            CurrencyCode     = r.CurrencyCode,
            CreatedAt        = r.CreatedAt,
        }).ToList();

        return new PagedResult<TravelExpenseReportDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
