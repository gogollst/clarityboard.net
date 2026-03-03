using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.salary.view")]
public record GetSalaryHistoryQuery(Guid EmployeeId) : IRequest<List<SalaryDto>>;

public record SalaryDto
{
    public Guid Id { get; init; }
    public string SalaryType { get; init; } = string.Empty;
    public int GrossAmountCents { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public int BonusAmountCents { get; init; }
    public string BonusCurrencyCode { get; init; } = string.Empty;
    public int PaymentCycleMonths { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public string ChangeReason { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
}

public class GetSalaryHistoryQueryHandler : IRequestHandler<GetSalaryHistoryQuery, List<SalaryDto>>
{
    private readonly IAppDbContext _db;

    public GetSalaryHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<SalaryDto>> Handle(GetSalaryHistoryQuery request, CancellationToken cancellationToken)
    {
        var employeeExists = await _db.Employees
            .AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (!employeeExists)
            throw new NotFoundException("Employee", request.EmployeeId);

        var salaries = await _db.SalaryHistories
            .Where(s => s.EmployeeId == request.EmployeeId)
            .OrderByDescending(s => s.ValidFrom)
            .Select(s => new SalaryDto
            {
                Id                = s.Id,
                SalaryType        = s.SalaryType.ToString(),
                GrossAmountCents  = s.GrossAmountCents,
                CurrencyCode      = s.CurrencyCode,
                BonusAmountCents  = s.BonusAmountCents,
                BonusCurrencyCode = s.BonusCurrencyCode,
                PaymentCycleMonths = s.PaymentCycleMonths,
                ValidFrom         = s.ValidFrom,
                ValidTo           = s.ValidTo,
                ChangeReason      = s.ChangeReason,
                IsCurrent         = s.ValidTo == null,
            })
            .ToListAsync(cancellationToken);

        return salaries;
    }
}
