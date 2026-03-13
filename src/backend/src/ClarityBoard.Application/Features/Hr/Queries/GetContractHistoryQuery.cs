using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetContractHistoryQuery(Guid EmployeeId) : IRequest<List<ContractDto>>;

public record ContractDocumentDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}

public record ContractDto
{
    public Guid Id { get; init; }
    public string ContractType { get; init; } = string.Empty;
    public decimal WeeklyHours { get; init; }
    public int WorkdaysPerWeek { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateOnly? ProbationEndDate { get; init; }
    public int EmployeeNoticeWeeks { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public string ChangeReason { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }

    // Salary fields
    public string SalaryType { get; init; } = string.Empty;
    public int GrossAmountCents { get; init; }
    public string CurrencyCode { get; init; } = "EUR";
    public int BonusAmountCents { get; init; }
    public string BonusCurrencyCode { get; init; } = "EUR";
    public int PaymentCycleMonths { get; init; }

    // Extended fields
    public string? EmploymentType { get; init; }
    public int EmployerNoticeWeeks { get; init; }
    public int AnnualVacationDays { get; init; }
    public string? FixedTermReason { get; init; }
    public int FixedTermExtensionCount { get; init; }
    public bool Has13thSalary { get; init; }
    public bool HasVacationBonus { get; init; }
    public int VariablePayCents { get; init; }
    public string? VariablePayDescription { get; init; }
    public string? Notes { get; init; }

    public List<ContractDocumentDto> Documents { get; init; } = [];
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
                Id                      = c.Id,
                ContractType            = c.ContractType.ToString(),
                WeeklyHours             = c.WeeklyHours,
                WorkdaysPerWeek         = c.WorkdaysPerWeek,
                StartDate               = c.StartDate,
                EndDate                 = c.EndDate,
                ProbationEndDate        = c.ProbationEndDate,
                EmployeeNoticeWeeks     = c.EmployeeNoticeWeeks,
                ValidFrom               = c.ValidFrom,
                ValidTo                 = c.ValidTo,
                ChangeReason            = c.ChangeReason,
                IsCurrent               = c.ValidTo == null,
                SalaryType              = c.SalaryType.ToString(),
                GrossAmountCents        = c.GrossAmountCents,
                CurrencyCode            = c.CurrencyCode,
                BonusAmountCents        = c.BonusAmountCents,
                BonusCurrencyCode       = c.BonusCurrencyCode,
                PaymentCycleMonths      = c.PaymentCycleMonths,
                EmploymentType          = c.EmploymentType != null ? c.EmploymentType.ToString() : null,
                EmployerNoticeWeeks     = c.EmployerNoticeWeeks,
                AnnualVacationDays      = c.AnnualVacationDays,
                FixedTermReason         = c.FixedTermReason,
                FixedTermExtensionCount = c.FixedTermExtensionCount,
                Has13thSalary           = c.Has13thSalary,
                HasVacationBonus        = c.HasVacationBonus,
                VariablePayCents        = c.VariablePayCents,
                VariablePayDescription  = c.VariablePayDescription,
                Notes                   = c.Notes,
                Documents               = _db.EmployeeDocuments
                    .Where(d => d.ContractId == c.Id)
                    .Select(d => new ContractDocumentDto
                    {
                        Id           = d.Id,
                        Title        = d.Title,
                        FileName     = d.FileName,
                        DocumentType = d.DocumentType.ToString(),
                        UploadedAt   = d.UploadedAt,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return contracts;
    }
}
