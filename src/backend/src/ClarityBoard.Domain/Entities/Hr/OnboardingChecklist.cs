namespace ClarityBoard.Domain.Entities.Hr;

public class OnboardingChecklist
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public OnboardingStatus Status { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public ICollection<OnboardingTask> Tasks { get; private set; } = [];

    private OnboardingChecklist() { }

    public static OnboardingChecklist Create(Guid employeeId, string title, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        return new OnboardingChecklist
        {
            Id         = Guid.NewGuid(),
            EmployeeId = employeeId,
            Title      = title,
            Status     = OnboardingStatus.InProgress,
            CreatedBy  = createdBy,
            CreatedAt  = DateTime.UtcNow,
        };
    }

    public void Complete()
    {
        Status      = OnboardingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        Status      = OnboardingStatus.InProgress;
        CompletedAt = null;
    }
}

public class OnboardingTask
{
    public Guid Id { get; private set; }
    public Guid ChecklistId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public int SortOrder { get; private set; }

    private OnboardingTask() { }

    public static OnboardingTask Create(Guid checklistId, string title, string? description,
        DateOnly? dueDate, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        return new OnboardingTask
        {
            Id          = Guid.NewGuid(),
            ChecklistId = checklistId,
            Title       = title,
            Description = description,
            IsCompleted = false,
            DueDate     = dueDate,
            SortOrder   = sortOrder,
        };
    }

    public void Complete(Guid completedBy)
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completedBy;
    }

    public void Reopen()
    {
        IsCompleted = false;
        CompletedAt = null;
        CompletedBy = null;
    }
}

public enum OnboardingStatus { InProgress, Completed }
