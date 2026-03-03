namespace ClarityBoard.Domain.Entities.Hr;

public class Employee
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string EmployeeNumber { get; private set; } = string.Empty;
    public EmployeeType EmployeeType { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public string TaxId { get; private set; } = string.Empty;
    public string? SocialSecurityNumber { get; private set; }
    public EmployeeStatus Status { get; private set; }
    public DateOnly HireDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }
    public string? TerminationReason { get; private set; }
    public Guid? ManagerId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public ICollection<SalaryHistory> SalaryHistories { get; private set; } = [];
    public ICollection<Contract> Contracts { get; private set; } = [];
    public ICollection<LeaveRequest> LeaveRequests { get; private set; } = [];

    private Employee() { }

    public static Employee Create(Guid entityId, string employeeNumber, EmployeeType type,
        string firstName, string lastName, DateOnly dateOfBirth, string taxId, DateOnly hireDate,
        Guid? managerId = null, Guid? departmentId = null)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(employeeNumber)) throw new ArgumentException("Employee number is required.", nameof(employeeNumber));
        if (string.IsNullOrWhiteSpace(taxId)) throw new ArgumentException("Tax ID is required.", nameof(taxId));
        return new Employee
        {
            Id             = Guid.NewGuid(),
            EntityId       = entityId,
            EmployeeNumber = employeeNumber,
            EmployeeType   = type,
            FirstName      = firstName,
            LastName       = lastName,
            DateOfBirth    = dateOfBirth,
            TaxId          = taxId,
            Status         = EmployeeStatus.Active,
            HireDate       = hireDate,
            ManagerId      = managerId,
            DepartmentId   = departmentId,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow,
        };
    }

    public string FullName => $"{FirstName} {LastName}";

    public void UpdateBasicInfo(string firstName, string lastName, Guid? managerId, Guid? departmentId)
    {
        FirstName    = firstName;
        LastName     = lastName;
        ManagerId    = managerId;
        DepartmentId = departmentId;
        UpdatedAt    = DateTime.UtcNow;
    }

    public void Terminate(DateOnly terminationDate, string reason)
    {
        if (Status == EmployeeStatus.Terminated)
            throw new InvalidOperationException("Employee is already terminated.");
        Status            = EmployeeStatus.Terminated;
        TerminationDate   = terminationDate;
        TerminationReason = reason;
        UpdatedAt         = DateTime.UtcNow;
    }

    public void SetStatus(EmployeeStatus status)
    {
        Status    = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
