using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record CancelRevenueScheduleCommand(
    Guid EntityId,
    Guid DocumentId) : ICommand<CancelRevenueScheduleResult>, IEntityScoped;

public record CancelRevenueScheduleResult(int CancelledCount);

public class CancelRevenueScheduleCommandValidator : AbstractValidator<CancelRevenueScheduleCommand>
{
    public CancelRevenueScheduleCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.DocumentId).NotEmpty();
    }
}

public class CancelRevenueScheduleCommandHandler
    : IRequestHandler<CancelRevenueScheduleCommand, CancelRevenueScheduleResult>
{
    private readonly IAppDbContext _db;

    public CancelRevenueScheduleCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<CancelRevenueScheduleResult> Handle(
        CancelRevenueScheduleCommand request, CancellationToken ct)
    {
        var plannedEntries = await _db.RevenueScheduleEntries
            .Where(e => e.EntityId == request.EntityId
                && e.DocumentId == request.DocumentId
                && e.Status == "planned")
            .ToListAsync(ct);

        foreach (var entry in plannedEntries)
        {
            entry.Cancel();
        }

        await _db.SaveChangesAsync(ct);

        return new CancelRevenueScheduleResult(plannedEntries.Count);
    }
}
