using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Queries;

[RequirePermission("hr.view")]
public record GetTravelExpenseQuery(Guid ReportId) : IRequest<TravelExpenseReportDetailDto>;

public record TravelExpenseReportDetailDto
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
    public List<TravelExpenseItemDto> Items { get; init; } = [];
}

public record TravelExpenseItemDto
{
    public Guid Id { get; init; }
    public Guid ReportId { get; init; }
    public string ExpenseType { get; init; } = string.Empty;
    public DateOnly ExpenseDate { get; init; }
    public string Description { get; init; } = string.Empty;
    public int OriginalAmountCents { get; init; }
    public string OriginalCurrencyCode { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
    public DateOnly ExchangeRateDate { get; init; }
    public int AmountCents { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal? VatRatePercent { get; init; }
    public bool IsDeductible { get; init; }
}

public class GetTravelExpenseQueryValidator : AbstractValidator<GetTravelExpenseQuery>
{
    public GetTravelExpenseQueryValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
    }
}

public class GetTravelExpenseQueryHandler : IRequestHandler<GetTravelExpenseQuery, TravelExpenseReportDetailDto>
{
    private readonly IAppDbContext _db;

    public GetTravelExpenseQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<TravelExpenseReportDetailDto> Handle(
        GetTravelExpenseQuery request, CancellationToken cancellationToken)
    {
        var report = await _db.TravelExpenseReports
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken)
            ?? throw new NotFoundException("TravelExpenseReport", request.ReportId);

        var employee = await _db.Employees
            .Where(e => e.Id == report.EmployeeId)
            .Select(e => new { e.Id, e.FirstName, e.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        var fullName = employee is not null
            ? $"{employee.FirstName} {employee.LastName}"
            : string.Empty;

        var items = report.Items
            .OrderBy(i => i.ExpenseDate)
            .Select(i => new TravelExpenseItemDto
            {
                Id                   = i.Id,
                ReportId             = i.ReportId,
                ExpenseType          = i.ExpenseType.ToString(),
                ExpenseDate          = i.ExpenseDate,
                Description          = i.Description,
                OriginalAmountCents  = i.OriginalAmountCents,
                OriginalCurrencyCode = i.OriginalCurrencyCode,
                ExchangeRate         = i.ExchangeRate,
                ExchangeRateDate     = i.ExchangeRateDate,
                AmountCents          = i.AmountCents,
                CurrencyCode         = i.CurrencyCode,
                VatRatePercent       = i.VatRatePercent,
                IsDeductible         = i.IsDeductible,
            })
            .ToList();

        return new TravelExpenseReportDetailDto
        {
            Id               = report.Id,
            EmployeeId       = report.EmployeeId,
            EmployeeFullName = fullName,
            Title            = report.Title,
            TripStartDate    = report.TripStartDate,
            TripEndDate      = report.TripEndDate,
            Destination      = report.Destination,
            BusinessPurpose  = report.BusinessPurpose,
            Status           = report.Status.ToString(),
            TotalAmountCents = report.TotalAmountCents,
            CurrencyCode     = report.CurrencyCode,
            CreatedAt        = report.CreatedAt,
            Items            = items,
        };
    }
}
