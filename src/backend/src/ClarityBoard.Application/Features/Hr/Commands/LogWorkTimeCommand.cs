using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Exceptions;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Hr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Hr.Commands;

[RequirePermission("hr.self")]
public record LogWorkTimeCommand : IRequest<Guid>
{
    public required Guid EmployeeId { get; init; }
    public required DateOnly Date { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public int BreakMinutes { get; init; }
    public int? TotalMinutes { get; init; }
    public string EntryType { get; init; } = "Work";
    public string? ProjectCode { get; init; }
    public string? Notes { get; init; }
}

public class LogWorkTimeCommandValidator : AbstractValidator<LogWorkTimeCommand>
{
    public LogWorkTimeCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.BreakMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ProjectCode).MaximumLength(50).When(x => x.ProjectCode != null);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
        RuleFor(x => x.EntryType)
            .Must(t => t == "Work" || t == "Overtime" || t == "OnCall")
            .WithMessage("EntryType must be 'Work', 'Overtime', or 'OnCall'.")
            .When(x => !string.IsNullOrEmpty(x.EntryType));
    }
}

public class LogWorkTimeCommandHandler : IRequestHandler<LogWorkTimeCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LogWorkTimeCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(LogWorkTimeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.EmployeeId);

        // TODO (Fix 4): verify that _currentUser owns this employee record (or has hr.manage permission)
        // once ICurrentUser exposes an EmployeeId / HrEmployeeId property.
        _ = employee; // suppress unused-variable warning until ownership check is added

        if (request.StartTime.HasValue && request.EndTime.HasValue
            && request.EndTime.Value <= request.StartTime.Value)
        {
            throw new InvalidOperationException("EndTime must be greater than StartTime.");
        }

        int totalMinutes;
        if (request.StartTime.HasValue && request.EndTime.HasValue)
        {
            var duration = request.EndTime.Value.ToTimeSpan() - request.StartTime.Value.ToTimeSpan();
            totalMinutes = (int)duration.TotalMinutes - request.BreakMinutes;
        }
        else if (request.TotalMinutes.HasValue)
        {
            totalMinutes = request.TotalMinutes.Value;
        }
        else
        {
            throw new InvalidOperationException("Either StartTime + EndTime or TotalMinutes must be provided.");
        }

        if (totalMinutes < 0)
            throw new InvalidOperationException("TotalMinutes cannot be negative.");

        var entryType = Enum.Parse<EntryType>(request.EntryType, ignoreCase: true);

        // Fix 7: prevent duplicate work time entries for the same employee/date/type
        var exists = await _db.WorkTimeEntries
            .AnyAsync(e => e.EmployeeId == request.EmployeeId
                        && e.Date == request.Date
                        && e.EntryType == entryType, cancellationToken);
        if (exists)
            throw new InvalidOperationException(
                $"A {entryType} entry for {request.Date:yyyy-MM-dd} already exists for this employee.");

        var entry = WorkTimeEntry.Create(
            employeeId:   request.EmployeeId,
            date:         request.Date,
            totalMinutes: totalMinutes,
            createdBy:    _currentUser.UserId,
            startTime:    request.StartTime,
            endTime:      request.EndTime,
            breakMinutes: request.BreakMinutes,
            type:         entryType,
            projectCode:  request.ProjectCode,
            notes:        request.Notes);

        _db.WorkTimeEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }
}
