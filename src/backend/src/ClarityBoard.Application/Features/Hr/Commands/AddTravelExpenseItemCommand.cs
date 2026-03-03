using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.self")]
public record AddTravelExpenseItemCommand : IRequest<Guid>
{
    public required Guid ReportId { get; init; }
    public required string ExpenseType { get; init; }
    public required DateOnly ExpenseDate { get; init; }
    public required string Description { get; init; }
    public required int OriginalAmountCents { get; init; }
    public required string OriginalCurrencyCode { get; init; }
    public decimal ExchangeRate { get; init; } = 1.0m;
    public required DateOnly ExchangeRateDate { get; init; }
    public decimal? VatRatePercent { get; init; }
    public bool IsDeductible { get; init; } = true;
}

public class AddTravelExpenseItemCommandValidator : AbstractValidator<AddTravelExpenseItemCommand>
{
    public AddTravelExpenseItemCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.ExpenseType).NotEmpty()
            .Must(t => Enum.TryParse<ExpenseType>(t, ignoreCase: true, out _))
            .WithMessage("ExpenseType must be one of: Accommodation, Transport, Meal, Other.");
        RuleFor(x => x.ExpenseDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OriginalAmountCents).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OriginalCurrencyCode).NotEmpty().MaximumLength(3);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.ExchangeRateDate).NotEmpty();
        RuleFor(x => x.VatRatePercent).InclusiveBetween(0m, 100m).When(x => x.VatRatePercent.HasValue);
    }
}

public class AddTravelExpenseItemCommandHandler : IRequestHandler<AddTravelExpenseItemCommand, Guid>
{
    private readonly IAppDbContext _db;

    public AddTravelExpenseItemCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(AddTravelExpenseItemCommand request, CancellationToken cancellationToken)
    {
        var report = await _db.TravelExpenseReports
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken)
            ?? throw new NotFoundException("TravelExpenseReport", request.ReportId);

        if (report.Status != TravelExpenseStatus.Draft)
            throw new InvalidOperationException("Items can only be added to draft reports.");

        var expenseType = Enum.Parse<ExpenseType>(request.ExpenseType, ignoreCase: true);

        var item = TravelExpenseItem.Create(
            reportId:             request.ReportId,
            type:                 expenseType,
            expenseDate:          request.ExpenseDate,
            description:          request.Description,
            originalAmountCents:  request.OriginalAmountCents,
            originalCurrencyCode: request.OriginalCurrencyCode,
            exchangeRate:         request.ExchangeRate,
            exchangeRateDate:     request.ExchangeRateDate,
            isDeductible:         request.IsDeductible,
            vatRate:              request.VatRatePercent);

        _db.TravelExpenseItems.Add(item);

        // Recalculate total (existing items + new item)
        var existingTotal = report.Items.Sum(i => i.AmountCents);
        report.UpdateTotal(existingTotal + item.AmountCents);

        await _db.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
