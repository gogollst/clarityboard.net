using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record AddOnboardingTaskCommand : IRequest<Guid>
{
    public required Guid ChecklistId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateOnly? DueDate { get; init; }
    public int SortOrder { get; init; }
}

public class AddOnboardingTaskCommandValidator : AbstractValidator<AddOnboardingTaskCommand>
{
    public AddOnboardingTaskCommandValidator()
    {
        RuleFor(x => x.ChecklistId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
    }
}

public class AddOnboardingTaskCommandHandler : IRequestHandler<AddOnboardingTaskCommand, Guid>
{
    private readonly IAppDbContext _db;

    public AddOnboardingTaskCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> Handle(AddOnboardingTaskCommand request, CancellationToken cancellationToken)
    {
        var checklistExists = await _db.OnboardingChecklists.AnyAsync(c => c.Id == request.ChecklistId, cancellationToken);
        if (!checklistExists)
            throw new NotFoundException("OnboardingChecklist", request.ChecklistId);

        var task = OnboardingTask.Create(
            checklistId: request.ChecklistId,
            title:       request.Title,
            description: request.Description,
            dueDate:     request.DueDate,
            sortOrder:   request.SortOrder);

        _db.OnboardingTasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);
        return task.Id;
    }
}
