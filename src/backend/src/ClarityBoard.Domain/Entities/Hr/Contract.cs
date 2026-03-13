namespace ClarityBoard.Domain.Entities.Hr;

public class Contract
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public ContractType ContractType { get; private set; }
    public decimal WeeklyHours { get; private set; }
    public int WorkdaysPerWeek { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? ProbationEndDate { get; private set; }
    public int EmployeeNoticeWeeks { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string ChangeReason { get; private set; } = string.Empty;

    // Salary fields (absorbed from SalaryHistory)
    public SalaryType SalaryType { get; private set; }
    public int GrossAmountCents { get; private set; }
    public string CurrencyCode { get; private set; } = "EUR";
    public int BonusAmountCents { get; private set; }
    public string BonusCurrencyCode { get; private set; } = "EUR";
    public int PaymentCycleMonths { get; private set; } = 12;

    // Extended contract fields
    public EmploymentType? EmploymentType { get; private set; }
    public int EmployerNoticeWeeks { get; private set; } = 4;
    public int AnnualVacationDays { get; private set; } = 20;
    public string? FixedTermReason { get; private set; }
    public int FixedTermExtensionCount { get; private set; }
    public bool Has13thSalary { get; private set; }
    public bool HasVacationBonus { get; private set; }
    public int VariablePayCents { get; private set; }
    public string? VariablePayDescription { get; private set; }
    public string? Notes { get; private set; }

    private Contract() { }

    public static Contract Create(
        Guid employeeId, ContractType type, decimal weeklyHours, int workdaysPerWeek,
        DateOnly startDate, int employeeNoticeWeeks, Guid createdBy, string changeReason,
        DateTime validFrom, SalaryType salaryType, int grossAmountCents,
        string currencyCode = "EUR", int bonusAmountCents = 0, string bonusCurrencyCode = "EUR",
        int paymentCycleMonths = 12, EmploymentType? employmentType = null,
        int employerNoticeWeeks = 4, int annualVacationDays = 20,
        string? fixedTermReason = null, int fixedTermExtensionCount = 0,
        bool has13thSalary = false, bool hasVacationBonus = false,
        int variablePayCents = 0, string? variablePayDescription = null,
        string? notes = null, DateOnly? endDate = null, DateOnly? probationEndDate = null)
    => new()
    {
        Id                      = Guid.NewGuid(),
        EmployeeId              = employeeId,
        ContractType            = type,
        WeeklyHours             = weeklyHours,
        WorkdaysPerWeek         = workdaysPerWeek,
        StartDate               = startDate,
        EndDate                 = endDate,
        ProbationEndDate        = probationEndDate,
        EmployeeNoticeWeeks     = employeeNoticeWeeks,
        ValidFrom               = validFrom,
        CreatedBy               = createdBy,
        ChangeReason            = changeReason,
        SalaryType              = salaryType,
        GrossAmountCents        = grossAmountCents,
        CurrencyCode            = currencyCode,
        BonusAmountCents        = bonusAmountCents,
        BonusCurrencyCode       = bonusCurrencyCode,
        PaymentCycleMonths      = paymentCycleMonths,
        EmploymentType          = employmentType,
        EmployerNoticeWeeks     = employerNoticeWeeks,
        AnnualVacationDays      = annualVacationDays,
        FixedTermReason         = fixedTermReason,
        FixedTermExtensionCount = fixedTermExtensionCount,
        Has13thSalary           = has13thSalary,
        HasVacationBonus        = hasVacationBonus,
        VariablePayCents        = variablePayCents,
        VariablePayDescription  = variablePayDescription,
        Notes                   = notes,
    };

    public void Update(
        ContractType type, decimal weeklyHours, int workdaysPerWeek,
        DateOnly startDate, int employeeNoticeWeeks,
        SalaryType salaryType, int grossAmountCents,
        string currencyCode, int bonusAmountCents, string bonusCurrencyCode,
        int paymentCycleMonths, EmploymentType? employmentType,
        int employerNoticeWeeks, int annualVacationDays,
        string? fixedTermReason, int fixedTermExtensionCount,
        bool has13thSalary, bool hasVacationBonus,
        int variablePayCents, string? variablePayDescription,
        string? notes, DateOnly? endDate, DateOnly? probationEndDate)
    {
        if (ValidTo != null)
            throw new InvalidOperationException("Cannot update a closed contract.");

        ContractType            = type;
        WeeklyHours             = weeklyHours;
        WorkdaysPerWeek         = workdaysPerWeek;
        StartDate               = startDate;
        EndDate                 = endDate;
        ProbationEndDate        = probationEndDate;
        EmployeeNoticeWeeks     = employeeNoticeWeeks;
        SalaryType              = salaryType;
        GrossAmountCents        = grossAmountCents;
        CurrencyCode            = currencyCode;
        BonusAmountCents        = bonusAmountCents;
        BonusCurrencyCode       = bonusCurrencyCode;
        PaymentCycleMonths      = paymentCycleMonths;
        EmploymentType          = employmentType;
        EmployerNoticeWeeks     = employerNoticeWeeks;
        AnnualVacationDays      = annualVacationDays;
        FixedTermReason         = fixedTermReason;
        FixedTermExtensionCount = fixedTermExtensionCount;
        Has13thSalary           = has13thSalary;
        HasVacationBonus        = hasVacationBonus;
        VariablePayCents        = variablePayCents;
        VariablePayDescription  = variablePayDescription;
        Notes                   = notes;
    }

    public void Close(DateTime validTo)
    {
        if (validTo <= ValidFrom)
            throw new ArgumentException("ValidTo must be after ValidFrom.", nameof(validTo));
        ValidTo = validTo;
    }
}
