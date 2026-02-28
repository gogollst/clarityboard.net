using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Entity.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Entity.Commands;

[RequirePermission("entity.edit")]
public record UpdateEntityCommand : IRequest<LegalEntityDto>
{
    public Guid Id { get; init; } // Set from route by controller
    public required string Name { get; init; }
    public required string LegalForm { get; init; }
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }
    public string Country { get; init; } = "DE";
    public string Currency { get; init; } = "EUR";
    public string ChartOfAccounts { get; init; } = "SKR03";
    public int FiscalYearStartMonth { get; init; } = 1;
    public Guid? ParentEntityId { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? TaxId { get; init; }
    public string? VatId { get; init; }
    public string? DatevClientNumber { get; init; }
    public string? DatevConsultantNumber { get; init; }
    public Guid? ManagingDirectorId { get; init; }
}

public class UpdateEntityCommandValidator : AbstractValidator<UpdateEntityCommand>
{
    public UpdateEntityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.LegalForm).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(256);
        RuleFor(x => x.City).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(3);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
        RuleFor(x => x.ChartOfAccounts).Must(v => v is "SKR03" or "SKR04")
            .WithMessage("ChartOfAccounts must be 'SKR03' or 'SKR04'.");
        RuleFor(x => x.FiscalYearStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.TaxId).MaximumLength(50).When(x => x.TaxId != null);
        RuleFor(x => x.VatId).MaximumLength(50).When(x => x.VatId != null);
        RuleFor(x => x.RegistrationNumber).MaximumLength(100).When(x => x.RegistrationNumber != null);
        RuleFor(x => x.DatevClientNumber).MaximumLength(10).When(x => x.DatevClientNumber != null);
        RuleFor(x => x.DatevConsultantNumber).MaximumLength(10).When(x => x.DatevConsultantNumber != null);
        RuleFor(x => x.ManagingDirectorId)
            .Must(id => id == null || id != Guid.Empty)
            .WithMessage("ManagingDirectorId must be null or a valid non-empty GUID.")
            .When(x => x.ManagingDirectorId.HasValue);
    }
}

public class UpdateEntityCommandHandler : IRequestHandler<UpdateEntityCommand, LegalEntityDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;

    public UpdateEntityCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IAuditService auditService)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<LegalEntityDto> Handle(UpdateEntityCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.LegalEntities
            .FirstOrDefaultAsync(le => le.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Entity with id '{request.Id}' was not found.");

        // Check for duplicate name (excluding current entity)
        var nameExists = await _db.LegalEntities
            .AnyAsync(le => le.Name == request.Name && le.Id != request.Id, cancellationToken);
        if (nameExists)
            throw new InvalidOperationException($"An entity with name '{request.Name}' already exists.");

        // Validate parent entity exists if specified and is not self-referencing
        if (request.ParentEntityId.HasValue)
        {
            if (request.ParentEntityId.Value == request.Id)
                throw new InvalidOperationException("An entity cannot be its own parent.");
            var parentExists = await _db.LegalEntities
                .AnyAsync(le => le.Id == request.ParentEntityId.Value, cancellationToken);
            if (!parentExists)
                throw new InvalidOperationException("The specified parent entity does not exist.");
        }

        // Validate managing director user exists if specified
        if (request.ManagingDirectorId.HasValue)
        {
            var userExists = await _db.Users
                .AnyAsync(u => u.Id == request.ManagingDirectorId.Value, cancellationToken);
            if (!userExists)
                throw new InvalidOperationException("The specified managing director user does not exist.");
        }

        entity.Update(
            name: request.Name,
            legalForm: request.LegalForm,
            street: request.Street,
            city: request.City,
            postalCode: request.PostalCode,
            country: request.Country,
            currency: request.Currency,
            chartOfAccounts: request.ChartOfAccounts,
            fiscalYearStartMonth: request.FiscalYearStartMonth,
            parentEntityId: request.ParentEntityId,
            registrationNumber: request.RegistrationNumber,
            taxId: request.TaxId,
            vatId: request.VatId,
            datevClientNumber: request.DatevClientNumber,
            datevConsultantNumber: request.DatevConsultantNumber,
            managingDirectorId: request.ManagingDirectorId);

        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            entityId: entity.Id,
            action: "update",
            tableName: "legal_entities",
            recordId: entity.Id.ToString(),
            oldValues: null,
            newValues: $"{{\"name\":\"{entity.Name}\",\"legalForm\":\"{entity.LegalForm}\"}}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);

        string? managingDirectorName = null;
        if (entity.ManagingDirectorId.HasValue)
        {
            var director = await _db.Users
                .Where(u => u.Id == entity.ManagingDirectorId.Value)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync(cancellationToken);
            managingDirectorName = director is not null
                ? $"{director.FirstName} {director.LastName}"
                : null;
        }

        return new LegalEntityDto
        {
            Id = entity.Id,
            Name = entity.Name,
            LegalForm = entity.LegalForm,
            RegistrationNumber = entity.RegistrationNumber,
            TaxId = entity.TaxId,
            VatId = entity.VatId,
            Street = entity.Street,
            City = entity.City,
            PostalCode = entity.PostalCode,
            Country = entity.Country,
            Currency = entity.Currency,
            ChartOfAccounts = entity.ChartOfAccounts,
            FiscalYearStartMonth = entity.FiscalYearStartMonth,
            ParentEntityId = entity.ParentEntityId,
            IsActive = entity.IsActive,
            DatevClientNumber = entity.DatevClientNumber,
            DatevConsultantNumber = entity.DatevConsultantNumber,
            ManagingDirectorId = entity.ManagingDirectorId,
            ManagingDirectorName = managingDirectorName,
            CreatedAt = entity.CreatedAt,
        };
    }
}

