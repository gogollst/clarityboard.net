using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Queries;

public record BusinessPartnerDto
{
    public required Guid Id { get; init; }
    public required string PartnerNumber { get; init; }
    public required string Name { get; init; }
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
    public required bool IsCreditor { get; init; }
    public required bool IsDebtor { get; init; }
    public Guid? DefaultExpenseAccountId { get; init; }
    public Guid? DefaultRevenueAccountId { get; init; }
    public Guid? ContactEmployeeId { get; init; }
    public string? ContactEmployeeName { get; init; }
    public required int PaymentTermDays { get; init; }
    public required bool IsActive { get; init; }
    public string? Notes { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record GetBusinessPartnerQuery(Guid EntityId, Guid Id) : IRequest<BusinessPartnerDto>, IEntityScoped;

public class GetBusinessPartnerQueryHandler : IRequestHandler<GetBusinessPartnerQuery, BusinessPartnerDto>
{
    private readonly IAppDbContext _db;

    public GetBusinessPartnerQueryHandler(IAppDbContext db) => _db = db;

    public async Task<BusinessPartnerDto> Handle(GetBusinessPartnerQuery request, CancellationToken ct)
    {
        var partner = await _db.BusinessPartners
            .Where(bp => bp.Id == request.Id && bp.EntityId == request.EntityId)
            .Select(bp => new BusinessPartnerDto
            {
                Id = bp.Id,
                PartnerNumber = bp.PartnerNumber,
                Name = bp.Name,
                TaxId = bp.TaxId,
                VatNumber = bp.VatNumber,
                Street = bp.Street,
                City = bp.City,
                PostalCode = bp.PostalCode,
                Country = bp.Country,
                Email = bp.Email,
                Phone = bp.Phone,
                BankName = bp.BankName,
                Iban = bp.Iban,
                Bic = bp.Bic,
                IsCreditor = bp.IsCreditor,
                IsDebtor = bp.IsDebtor,
                DefaultExpenseAccountId = bp.DefaultExpenseAccountId,
                DefaultRevenueAccountId = bp.DefaultRevenueAccountId,
                ContactEmployeeId = bp.ContactEmployeeId,
                ContactEmployeeName = bp.ContactEmployeeId.HasValue
                    ? _db.Employees
                        .Where(e => e.Id == bp.ContactEmployeeId.Value)
                        .Select(e => e.FirstName + " " + e.LastName)
                        .FirstOrDefault()
                    : null,
                PaymentTermDays = bp.PaymentTermDays,
                IsActive = bp.IsActive,
                Notes = bp.Notes,
                CreatedAt = bp.CreatedAt,
                UpdatedAt = bp.UpdatedAt,
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Business partner not found.");

        return partner;
    }
}
