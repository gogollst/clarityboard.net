using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record CreateCostCenterCommand : IRequest<Guid>
{
    public required string Code { get; init; }
    public required string ShortName { get; init; }
    public string? Description { get; init; }
    public CostCenterType Type { get; init; } = CostCenterType.Other;
    public Guid? HrEmployeeId { get; init; }
    public Guid? HrDepartmentId { get; init; }
    public Guid? ParentId { get; init; }
}

public class CreateCostCenterCommandValidator : AbstractValidator<CreateCostCenterCommand>
{
    public CreateCostCenterCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ShortName).NotEmpty().MaximumLength(100);
    }
}

public class CreateCostCenterCommandHandler : IRequestHandler<CreateCostCenterCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateCostCenterCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCostCenterCommand request, CancellationToken ct)
    {
        var exists = await _db.CostCenters
            .AnyAsync(cc => cc.EntityId == _currentUser.EntityId && cc.Code == request.Code, ct);

        if (exists)
            throw new InvalidOperationException(
                $"Cost center with code '{request.Code}' already exists.");

        var cc = CostCenter.Create(
            entityId: _currentUser.EntityId,
            code: request.Code,
            shortName: request.ShortName,
            type: request.Type,
            hrEmployeeId: request.HrEmployeeId,
            hrDepartmentId: request.HrDepartmentId,
            description: request.Description,
            parentId: request.ParentId);

        _db.CostCenters.Add(cc);
        await _db.SaveChangesAsync(ct);

        return cc.Id;
    }
}
