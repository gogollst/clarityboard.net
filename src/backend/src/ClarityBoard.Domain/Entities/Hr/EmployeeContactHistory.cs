namespace ClarityBoard.Domain.Entities.Hr;

public class EmployeeContactHistory
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? PhoneWork { get; private set; }
    public string? PhoneMobile { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string ChangeReason { get; private set; } = string.Empty;

    private EmployeeContactHistory() { }

    public static EmployeeContactHistory Create(Guid employeeId, string email, Guid createdBy, string changeReason,
        string? phoneWork = null, string? phoneMobile = null,
        string? emergencyName = null, string? emergencyPhone = null)
    => new()
    {
        Id                   = Guid.NewGuid(),
        EmployeeId           = employeeId,
        Email                = email,
        PhoneWork            = phoneWork,
        PhoneMobile          = phoneMobile,
        EmergencyContactName  = emergencyName,
        EmergencyContactPhone = emergencyPhone,
        ValidFrom            = DateTime.UtcNow,
        CreatedBy            = createdBy,
        ChangeReason         = changeReason,
    };

    public void Close(DateTime validTo)
    {
        if (validTo <= ValidFrom)
            throw new ArgumentException("ValidTo must be after ValidFrom.", nameof(validTo));
        ValidTo = validTo;
    }
}
