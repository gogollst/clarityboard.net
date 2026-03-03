using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record CreateLeaveTypeCommand : IRequest<Guid>
{
    public required Guid EntityId { get; init; }
    public required string Name { get; init; }
    public required string Code { get; init; }
    public bool RequiresApproval { get; init; } = true;
    public bool IsDeductedFromBalance { get; init; } = true;
    public int? MaxDaysPerYear { get; init; }
    public string Color { get; init; } = "#3b82f6";
    public bool IsActive { get; init; } = true;
}

public class CreateLeaveTypeCommandValidator : AbstractValidator<CreateLeaveTypeCommand>
{
    public CreateLeaveTypeCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Color).MaximumLength(20).When(x => x.Color != null);
        RuleFor(x => x.MaxDaysPerYear).GreaterThan(0).When(x => x.MaxDaysPerYear.HasValue);
    }
}

public class CreateLeaveTypeCommandHandler : IRequestHandler<CreateLeaveTypeCommand, Guid>
{
    private readonly IAppDbContext _db;

    public CreateLeaveTypeCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(CreateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var leaveType = LeaveType.Create(
            entityId:              request.EntityId,
            name:                  request.Name,
            code:                  request.Code,
            requiresApproval:      request.RequiresApproval,
            isDeductedFromBalance: request.IsDeductedFromBalance,
            maxDaysPerYear:        request.MaxDaysPerYear,
            color:                 request.Color);

        _db.LeaveTypes.Add(leaveType);
        await _db.SaveChangesAsync(cancellationToken);

        return leaveType.Id;
    }
}
