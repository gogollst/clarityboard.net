using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetContractHistoryQuery(Guid EmployeeId) : IRequest<List<ContractDto>>;

public record ContractDto
{
    public Guid Id { get; init; }
    public string ContractType { get; init; } = string.Empty;
    public decimal WeeklyHours { get; init; }
    public int WorkdaysPerWeek { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public int NoticeWeeks { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public string ChangeReason { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
}

public class GetContractHistoryQueryHandler : IRequestHandler<GetContractHistoryQuery, List<ContractDto>>
{
    private readonly IAppDbContext _db;

    public GetContractHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<ContractDto>> Handle(GetContractHistoryQuery request, CancellationToken cancellationToken)
    {
        var employeeExists = await _db.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
            throw new NotFoundException("Employee", request.EmployeeId);

        var contracts = await _db.Contracts
            .Where(c => c.EmployeeId == request.EmployeeId)
            .OrderByDescending(c => c.ValidFrom)
            .Select(c => new ContractDto
            {
                Id               = c.Id,
                ContractType     = c.ContractType.ToString(),
                WeeklyHours      = c.WeeklyHours,
                WorkdaysPerWeek  = c.WorkdaysPerWeek,
                StartDate        = c.StartDate,
                EndDate          = c.EndDate,
                ProbationEndDate = c.ProbationEndDate,
                NoticeWeeks      = c.NoticeWeeks,
                ValidFrom        = c.ValidFrom,
                ValidTo          = c.ValidTo,
                ChangeReason     = c.ChangeReason,
                IsCurrent        = c.ValidTo == null,
            })
            .ToListAsync(cancellationToken);

        return contracts;
    }
}
