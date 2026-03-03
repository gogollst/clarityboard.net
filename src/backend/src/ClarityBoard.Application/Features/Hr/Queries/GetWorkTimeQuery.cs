using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetWorkTimeQuery : IRequest<PagedResult<WorkTimeEntryDto>>
{
    public required Guid EmployeeId { get; init; }
    /// <summary>Optional month filter in YYYY-MM format, e.g. "2026-03"</summary>
    public string? Month { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public record WorkTimeEntryDto
{
    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public int BreakMinutes { get; init; }
    public int TotalMinutes { get; init; }
    public string EntryType { get; init; } = string.Empty;
    public string? ProjectCode { get; init; }
    public string? Notes { get; init; }
    public string Status { get; init; } = string.Empty;
}

public class GetWorkTimeQueryValidator : AbstractValidator<GetWorkTimeQuery>
{
    public GetWorkTimeQueryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Month)
            .Matches(@"^\d{4}-\d{2}$")
            .WithMessage("Month must be in YYYY-MM format.")
            .When(x => !string.IsNullOrEmpty(x.Month));
    }
}

public class GetWorkTimeQueryHandler : IRequestHandler<GetWorkTimeQuery, PagedResult<WorkTimeEntryDto>>
{
    private readonly IAppDbContext _db;

    public GetWorkTimeQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<WorkTimeEntryDto>> Handle(GetWorkTimeQuery request, CancellationToken cancellationToken)
    {
        var query = _db.WorkTimeEntries
            .Where(e => e.EmployeeId == request.EmployeeId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Month)
            && int.TryParse(request.Month.Split('-')[0], out var year)
            && int.TryParse(request.Month.Split('-')[1], out var month))
        {
            query = query.Where(e => e.Date.Year == year && e.Date.Month == month);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.Date)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new WorkTimeEntryDto
            {
                Id           = e.Id,
                EmployeeId   = e.EmployeeId,
                Date         = e.Date,
                StartTime    = e.StartTime,
                EndTime      = e.EndTime,
                BreakMinutes = e.BreakMinutes,
                TotalMinutes = e.TotalMinutes,
                EntryType    = e.EntryType.ToString(),
                ProjectCode  = e.ProjectCode,
                Notes        = e.Notes,
                Status       = e.Status.ToString(),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkTimeEntryDto>
        {
            Items      = items,
            TotalCount = totalCount,
            Page       = request.Page,
            PageSize   = request.PageSize,
        };
    }
}
