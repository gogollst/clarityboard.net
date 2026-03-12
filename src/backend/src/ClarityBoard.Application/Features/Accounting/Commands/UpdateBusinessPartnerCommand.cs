using ClarityBoard.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record UpdateBusinessPartnerCommand : IRequest
{
    public required Guid Id { get; init; }
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

public class UpdateBusinessPartnerCommandValidator : AbstractValidator<UpdateBusinessPartnerCommand>
{
    public UpdateBusinessPartnerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x).Must(x => x.IsCreditor || x.IsDebtor)
            .WithMessage("At least one of IsCreditor or IsDebtor must be true.");
        RuleFor(x => x.PaymentTermDays).GreaterThanOrEqualTo(0);
    }
}

public class UpdateBusinessPartnerCommandHandler : IRequestHandler<UpdateBusinessPartnerCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateBusinessPartnerCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateBusinessPartnerCommand request, CancellationToken ct)
    {
        var partner = await _db.BusinessPartners
            .FirstOrDefaultAsync(bp => bp.Id == request.Id && bp.EntityId == _currentUser.EntityId, ct)
            ?? throw new InvalidOperationException("Business partner not found.");

        partner.Update(
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

        await _db.SaveChangesAsync(ct);
    }
}
