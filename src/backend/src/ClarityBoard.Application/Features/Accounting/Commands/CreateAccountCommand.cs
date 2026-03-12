using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

[RequirePermission("accounting.plan")]
public record CreateAccountCommand : IRequest<Guid>
{
    public required string AccountNumber { get; init; }
    public required string Name { get; init; }
    public required string AccountType { get; init; }
    public required int AccountClass { get; init; }
    public string? VatDefault { get; init; }
    public string? DatevAuto { get; init; }
    public string? CostCenterDefault { get; init; }
    public string? BwaLine { get; init; }
    public bool IsAutoPosting { get; init; }
}

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(10)
            .Matches(@"^\d+$").WithMessage("Account number must contain only digits.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AccountType)
            .Must(t => t is "asset" or "liability" or "equity" or "revenue" or "expense")
            .WithMessage("AccountType must be asset, liability, equity, revenue, or expense.");
        RuleFor(x => x.AccountClass).InclusiveBetween(0, 9);
        RuleFor(x => x.VatDefault).MaximumLength(10).When(x => x.VatDefault != null);
        RuleFor(x => x.BwaLine).MaximumLength(10).When(x => x.BwaLine != null);
    }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateAccountCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _db.Accounts
            .AnyAsync(a => a.EntityId == _currentUser.EntityId
                && a.AccountNumber == request.AccountNumber, cancellationToken);

        if (duplicate)
            throw new InvalidOperationException(
                $"Account number '{request.AccountNumber}' already exists for this entity.");

        var account = Account.Create(
            entityId: _currentUser.EntityId,
            accountNumber: request.AccountNumber,
            name: request.Name,
            accountType: request.AccountType,
            accountClass: (short)request.AccountClass,
            vatDefault: request.VatDefault,
            datevAuto: request.DatevAuto);

        // Set additional fields not covered by Create()
        account.Update(
            request.Name,
            request.VatDefault,
            request.CostCenterDefault,
            request.BwaLine,
            request.IsAutoPosting);

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);
        return account.Id;
    }
}
