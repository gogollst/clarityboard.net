using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.manage")]
public record CreateOnboardingChecklistCommand : IRequest<Guid>
{
    public required Guid EmployeeId { get; init; }
    public required string Title { get; init; }
    public IReadOnlyList<CreateOnboardingTaskItem> Tasks { get; init; } = [];
}

public record CreateOnboardingTaskItem
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateOnly? DueDate { get; init; }
    public int SortOrder { get; init; }
}

public class CreateOnboardingChecklistCommandValidator : AbstractValidator<CreateOnboardingChecklistCommand>
{
    public CreateOnboardingChecklistCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.Tasks).ChildRules(task =>
        {
            task.RuleFor(t => t.Title).NotEmpty().MaximumLength(200);
            task.RuleFor(t => t.Description).MaximumLength(1000).When(t => t.Description != null);
        });
    }
}

public class CreateOnboardingChecklistCommandHandler : IRequestHandler<CreateOnboardingChecklistCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateOnboardingChecklistCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateOnboardingChecklistCommand request, CancellationToken cancellationToken)
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == request.EmployeeId, cancellationToken);
        if (!employeeExists)
            throw new NotFoundException("Employee", request.EmployeeId);

        var checklist = OnboardingChecklist.Create(
            employeeId: request.EmployeeId,
            title:      request.Title,
            createdBy:  _currentUser.UserId);

        _db.OnboardingChecklists.Add(checklist);

        var sortOrder = 0;
        foreach (var item in request.Tasks)
        {
            var task = OnboardingTask.Create(
                checklistId: checklist.Id,
                title:       item.Title,
                description: item.Description,
                dueDate:     item.DueDate,
                sortOrder:   item.SortOrder > 0 ? item.SortOrder : sortOrder++);
            _db.OnboardingTasks.Add(task);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return checklist.Id;
    }
}
