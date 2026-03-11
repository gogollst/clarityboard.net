using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.self")]
public record CompleteOnboardingTaskCommand : IRequest<Unit>
{
    public required Guid ChecklistId { get; init; }
    public required Guid TaskId { get; init; }
}

public class CompleteOnboardingTaskCommandHandler : IRequestHandler<CompleteOnboardingTaskCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CompleteOnboardingTaskCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(CompleteOnboardingTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _db.OnboardingTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.ChecklistId == request.ChecklistId, cancellationToken)
            ?? throw new NotFoundException("OnboardingTask", request.TaskId);

        task.Complete(_currentUser.UserId);
        await _db.SaveChangesAsync(cancellationToken);

        // Auto-complete checklist if all tasks are done
        var allDone = await _db.OnboardingTasks
            .Where(t => t.ChecklistId == request.ChecklistId)
            .AllAsync(t => t.IsCompleted, cancellationToken);

        if (allDone)
        {
            var checklist = await _db.OnboardingChecklists
                .FirstOrDefaultAsync(c => c.Id == request.ChecklistId, cancellationToken);
            checklist?.Complete();
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
