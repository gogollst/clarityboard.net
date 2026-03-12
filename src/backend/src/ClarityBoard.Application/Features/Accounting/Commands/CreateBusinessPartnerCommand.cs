using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record CreateBusinessPartnerCommand : IRequest<Guid>
{
    public required string Name { get; init; }
    public required bool IsCreditor { get; init; }
    public required bool IsDebtor { get; init; }
    public string? TaxId { get; init; }
    public string? VatNumber { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? BankName { get; init; }
    public string? Iban { get; init; }
    public string? Bic { get; init; }
    public Guid? DefaultExpenseAccountId { get; init; }
    public Guid? DefaultRevenueAccountId { get; init; }
    public Guid? ContactEmployeeId { get; init; }
    public int PaymentTermDays { get; init; } = 30;
    public string? Notes { get; init; }
}

public class CreateBusinessPartnerCommandValidator : AbstractValidator<CreateBusinessPartnerCommand>
{
    public CreateBusinessPartnerCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x).Must(x => x.IsCreditor || x.IsDebtor)
            .WithMessage("At least one of IsCreditor or IsDebtor must be true.");
        RuleFor(x => x.TaxId).MaximumLength(50);
        RuleFor(x => x.VatNumber).MaximumLength(50);
        RuleFor(x => x.Street).MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).MaximumLength(2);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Iban).MaximumLength(34);
        RuleFor(x => x.Bic).MaximumLength(11);
        RuleFor(x => x.PaymentTermDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class CreateBusinessPartnerCommandHandler : IRequestHandler<CreateBusinessPartnerCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateBusinessPartnerCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateBusinessPartnerCommand request, CancellationToken ct)
    {
        var entityId = _currentUser.EntityId;

        var partner = BusinessPartner.Create(
            entityId: entityId,
            name: request.Name,
            isCreditor: request.IsCreditor,
            isDebtor: request.IsDebtor,
            taxId: request.TaxId,
            vatNumber: request.VatNumber,
            street: request.Street,
            city: request.City,
            postalCode: request.PostalCode,
            country: request.Country,
            email: request.Email,
            phone: request.Phone,
            bankName: request.BankName,
            iban: request.Iban,
            bic: request.Bic,
            defaultExpenseAccountId: request.DefaultExpenseAccountId,
            defaultRevenueAccountId: request.DefaultRevenueAccountId,
            contactEmployeeId: request.ContactEmployeeId,
            paymentTermDays: request.PaymentTermDays,
            notes: request.Notes);

        // Generate partner number
        var prefix = (request.IsCreditor, request.IsDebtor) switch
        {
            (true, true) => "KD",
            (true, false) => "K",
            _ => "D",
        };

        var lastNumber = await _db.BusinessPartners
            .Where(bp => bp.EntityId == entityId && bp.PartnerNumber.StartsWith(prefix + "-"))
            .OrderByDescending(bp => bp.PartnerNumber)
            .Select(bp => bp.PartnerNumber)
            .FirstOrDefaultAsync(ct);

        var nextSeq = 1;
        if (lastNumber is not null)
        {
            var numPart = lastNumber[(prefix.Length + 1)..];
            if (int.TryParse(numPart, out var parsed))
                nextSeq = parsed + 1;
        }

        partner.SetPartnerNumber($"{prefix}-{nextSeq:D5}");

        _db.BusinessPartners.Add(partner);
        await _db.SaveChangesAsync(ct);

        return partner.Id;
    }
}
