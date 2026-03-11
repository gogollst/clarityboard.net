using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record ReopenOnboardingTaskCommand : IRequest<Unit>
{
    public required Guid ChecklistId { get; init; }
    public required Guid TaskId { get; init; }
}

public class ReopenOnboardingTaskCommandHandler : IRequestHandler<ReopenOnboardingTaskCommand, Unit>
{
    private readonly IAppDbContext _db;

    public ReopenOnboardingTaskCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(ReopenOnboardingTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _db.OnboardingTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.ChecklistId == request.ChecklistId, cancellationToken)
            ?? throw new NotFoundException("OnboardingTask", request.TaskId);

        task.Reopen();

        // Revert checklist to InProgress if it was completed
        var checklist = await _db.OnboardingChecklists
            .FirstOrDefaultAsync(c => c.Id == request.ChecklistId, cancellationToken);
        if (checklist?.Status == Domain.Entities.Hr.OnboardingStatus.Completed)
            checklist.Reopen();

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
