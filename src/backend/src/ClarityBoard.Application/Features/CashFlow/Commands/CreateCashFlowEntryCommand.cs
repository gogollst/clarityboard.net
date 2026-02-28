using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.CashFlow.DTOs;
using ClarityBoard.Domain.Entities.CashFlow;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.CashFlow.Commands;

public record CreateCashFlowEntryCommand : IRequest<CashFlowEntryDto>
{
    public required DateOnly EntryDate { get; init; }
    public required string Category { get; init; }
    public required string Subcategory { get; init; }
    public required decimal Amount { get; init; }
    public string? Description { get; init; }
    public string? SourceType { get; init; }
    public string Currency { get; init; } = "EUR";
    public decimal ExchangeRate { get; init; } = 1.0m;
    public string Certainty { get; init; } = "confirmed";
}

public class CreateCashFlowEntryCommandValidator : AbstractValidator<CreateCashFlowEntryCommand>
{
    private static readonly string[] ValidCategories =
        ["operating_inflow", "operating_outflow", "investing", "financing"];

    private static readonly string[] ValidCertainties =
        ["confirmed", "probable", "possible"];

    public CreateCashFlowEntryCommandValidator()
    {
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(c => ValidCategories.Contains(c))
            .WithMessage($"Category must be one of: {string.Join(", ", ValidCategories)}.");
        RuleFor(x => x.Subcategory).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).NotEqual(0).WithMessage("Amount cannot be zero.");
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.ExchangeRate).GreaterThan(0);
        RuleFor(x => x.Certainty)
            .Must(c => ValidCertainties.Contains(c))
            .WithMessage($"Certainty must be one of: {string.Join(", ", ValidCertainties)}.");
    }
}

public class CreateCashFlowEntryCommandHandler : IRequestHandler<CreateCashFlowEntryCommand, CashFlowEntryDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateCashFlowEntryCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CashFlowEntryDto> Handle(
        CreateCashFlowEntryCommand request, CancellationToken cancellationToken)
    {
        var entityId = _currentUser.EntityId;

        var entry = CashFlowEntry.Create(
            entityId,
            request.EntryDate,
            request.Category,
            request.Subcategory,
            request.Amount,
            request.Description,
            request.SourceType ?? "manual",
            sourceRef: null,
            request.Currency,
            request.ExchangeRate);

        _db.CashFlowEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return new CashFlowEntryDto
        {
            Id = entry.Id,
            EntityId = entry.EntityId,
            EntryDate = entry.EntryDate,
            Category = entry.Category,
            Subcategory = entry.Subcategory,
            Amount = entry.Amount,
            Currency = entry.Currency,
            BaseAmount = entry.BaseAmount,
            SourceType = entry.SourceType,
            Description = entry.Description,
            IsRecurring = entry.IsRecurring,
            Certainty = entry.Certainty,
            CreatedAt = entry.CreatedAt,
        };
    }
}
