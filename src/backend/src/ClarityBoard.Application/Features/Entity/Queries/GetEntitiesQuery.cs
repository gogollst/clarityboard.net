using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Features.Entity.DTOs;
using ClarityBoard.Domain.Entities.Entity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Entity.Queries;

public record GetEntitiesQuery : IRequest<IReadOnlyList<LegalEntityDto>>;

public class GetEntitiesQueryHandler : IRequestHandler<GetEntitiesQuery, IReadOnlyList<LegalEntityDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetEntitiesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<LegalEntityDto>> Handle(
        GetEntitiesQuery request, CancellationToken cancellationToken)
    {
        // Admin users (with admin.users.view permission) must be able to see ALL entities
        // so they can assign any entity when managing users.
        // All other users only see entities they have a role in.
        IQueryable<Domain.Entities.Entity.LegalEntity> query;
        if (_currentUser.HasPermission("admin.users.view"))
        {
            query = _db.LegalEntities;
        }
        else
        {
            var entityIds = await _db.UserRoles
                .Where(ur => ur.UserId == _currentUser.UserId)
                .Select(ur => ur.EntityId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = _db.LegalEntities.Where(le => entityIds.Contains(le.Id));
        }

        return await query
            .OrderBy(le => le.Name)
            .Select(le => new LegalEntityDto
            {
                Id = le.Id,
                Name = le.Name,
                LegalForm = le.LegalForm,
                RegistrationNumber = le.RegistrationNumber,
                TaxId = le.TaxId,
                VatId = le.VatId,
                Street = le.Street,
                City = le.City,
                PostalCode = le.PostalCode,
                Country = le.Country,
                Currency = le.Currency,
                ChartOfAccounts = le.ChartOfAccounts,
                FiscalYearStartMonth = le.FiscalYearStartMonth,
                ParentEntityId = le.ParentEntityId,
                IsActive = le.IsActive,
                DatevClientNumber = le.DatevClientNumber,
                DatevConsultantNumber = le.DatevConsultantNumber,
                ManagingDirectorId = le.ManagingDirectorId,
                ManagingDirectorName = le.ManagingDirectorId == null
                    ? null
                    : _db.Users
                        .Where(u => u.Id == le.ManagingDirectorId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault(),
                CreatedAt = le.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
