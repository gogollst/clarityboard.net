using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Entity.DTOs;
using ClarityBoard.Domain.Entities.Entity;
using ClarityBoard.Domain.Entities.Hr;
using ClarityBoard.Domain.Entities.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Entity.Commands;

[RequirePermission("entity.create")]
public record CreateEntityCommand : IRequest<LegalEntityDto>
{
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
    public Guid? TemplateDepartmentEntityId { get; init; }
}

public class CreateEntityCommandValidator : AbstractValidator<CreateEntityCommand>
{
    public CreateEntityCommandValidator()
    {
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

public class CreateEntityCommandHandler : IRequestHandler<CreateEntityCommand, LegalEntityDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _auditService;
    private readonly IChartOfAccountsSeeder _seeder;

    public CreateEntityCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IAuditService auditService,
        IChartOfAccountsSeeder seeder)
    {
        _db = db;
        _currentUser = currentUser;
        _auditService = auditService;
        _seeder = seeder;
    }

    public async Task<LegalEntityDto> Handle(CreateEntityCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate name
        var nameExists = await _db.LegalEntities
            .AnyAsync(le => le.Name == request.Name, cancellationToken);
        if (nameExists)
            throw new InvalidOperationException($"An entity with name '{request.Name}' already exists.");

        // Validate parent entity exists if specified
        if (request.ParentEntityId.HasValue)
        {
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

        var entity = LegalEntity.Create(
            name: request.Name,
            legalForm: request.LegalForm,
            street: request.Street,
            city: request.City,
            postalCode: request.PostalCode,
            chartOfAccounts: request.ChartOfAccounts,
            currency: request.Currency,
            country: request.Country,
            fiscalYearStartMonth: request.FiscalYearStartMonth,
            parentEntityId: request.ParentEntityId,
            registrationNumber: request.RegistrationNumber,
            taxId: request.TaxId,
            vatId: request.VatId,
            datevClientNumber: request.DatevClientNumber,
            datevConsultantNumber: request.DatevConsultantNumber,
            managingDirectorId: request.ManagingDirectorId);

        _db.LegalEntities.Add(entity);

        // Auto-assign the creating user to the new entity with their current role
        var currentUserRole = await _db.UserRoles
            .Where(ur => ur.UserId == _currentUser.UserId)
            .Select(ur => ur.RoleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentUserRole != Guid.Empty)
        {
            var userRole = UserRole.Create(_currentUser.UserId, currentUserRole, entity.Id, _currentUser.UserId);
            _db.UserRoles.Add(userRole);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Seed chart of accounts for the new entity
        await _seeder.SeedAsync(entity.Id, request.ChartOfAccounts, cancellationToken);

        await _auditService.LogAsync(
            entityId: entity.Id,
            action: "create",
            tableName: "legal_entities",
            recordId: entity.Id.ToString(),
            oldValues: null,
            newValues: $"{{\"name\":\"{entity.Name}\",\"legalForm\":\"{entity.LegalForm}\"}}",
            userId: _currentUser.UserId,
            ipAddress: null,
            userAgent: null,
            ct: cancellationToken);

        if (request.TemplateDepartmentEntityId.HasValue)
        {
            var sourceDepts = await _db.Departments
                .Where(d => d.EntityId == request.TemplateDepartmentEntityId.Value && d.IsActive)
                .OrderBy(d => d.CreatedAt)
                .ToListAsync(cancellationToken);

            var idMap = new Dictionary<Guid, Guid>();
            foreach (var src in sourceDepts)
            {
                Guid? newParentId = src.ParentDepartmentId.HasValue && idMap.TryGetValue(src.ParentDepartmentId.Value, out var mappedParent)
                    ? mappedParent
                    : null;

                var newDept = Department.Create(
                    entityId: entity.Id,
                    name: src.Name,
                    code: src.Code,
                    parentDepartmentId: newParentId,
                    managerId: null,
                    description: src.Description);
                _db.Departments.Add(newDept);
                idMap[src.Id] = newDept.Id;
            }

            if (sourceDepts.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);
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
            ManagingDirectorName = null, // Not resolved here — use GetEntitiesQuery for display
            CreatedAt = entity.CreatedAt,
        };
    }
}
