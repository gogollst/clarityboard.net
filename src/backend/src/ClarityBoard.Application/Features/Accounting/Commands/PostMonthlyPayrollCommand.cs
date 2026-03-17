using ClarityBoard.Application.Common.Attributes;
using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Domain.Entities.Accounting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClarityBoard.Application.Features.Accounting.Commands;

public record PayrollPostingResult
{
    public int EmployeesProcessed { get; init; }
    public int JournalEntriesCreated { get; init; }
    public decimal TotalGrossSalary { get; init; }
    public decimal TotalSocialContributions { get; init; }
    public List<PayrollPostingError> Errors { get; init; } = [];
}

public record PayrollPostingError(string EmployeeName, string Error);

[RequirePermission("accounting.post")]
public record PostMonthlyPayrollCommand : IRequest<PayrollPostingResult>
{
    public required Guid EntityId { get; init; }
    public required int Year { get; init; }
    public required int Month { get; init; }
}

public class PostMonthlyPayrollCommandValidator : AbstractValidator<PostMonthlyPayrollCommand>
{
    public PostMonthlyPayrollCommandValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2099);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class PostMonthlyPayrollCommandHandler : IRequestHandler<PostMonthlyPayrollCommand, PayrollPostingResult>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    // Employer social contribution rate (~20% of gross in Germany: health, pension, unemployment, care)
    private const decimal SocialContributionRate = 0.2015m;

    // SKR03 account numbers
    private const string SalaryExpenseAccount = "4120";           // Gehälter
    private const string WagesExpenseAccount = "4100";            // Löhne
    private const string SocialExpenseAccount = "4130";           // Gesetzliche soziale Aufwendungen
    private const string SalaryPayableAccount = "1740";           // Verbindlichkeiten (salary payables)
    private const string SocialPayableAccount = "1741";           // Verbindlichkeiten soziale Sicherheit

    public PostMonthlyPayrollCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PayrollPostingResult> Handle(PostMonthlyPayrollCommand request, CancellationToken ct)
    {
        var entityId = request.EntityId;
        var entryDate = new DateOnly(request.Year, request.Month,
            DateTime.DaysInMonth(request.Year, request.Month)); // Last day of month

        // Check for open fiscal period
        var fiscalPeriod = await _db.FiscalPeriods
            .FirstOrDefaultAsync(fp =>
                fp.EntityId == entityId &&
                fp.StartDate <= entryDate &&
                fp.EndDate >= entryDate &&
                fp.Status == "open", ct)
            ?? throw new InvalidOperationException(
                $"No open fiscal period found for {request.Year}-{request.Month:D2}.");

        // Check if payroll already posted for this month
        var alreadyPosted = await _db.JournalEntries
            .AnyAsync(je =>
                je.EntityId == entityId &&
                je.SourceType == "payroll" &&
                je.SourceRef == $"{request.Year}-{request.Month:D2}", ct);
        if (alreadyPosted)
            throw new InvalidOperationException(
                $"Payroll for {request.Year}-{request.Month:D2} has already been posted.");

        // Resolve account IDs
        var accounts = await _db.Accounts
            .Where(a => a.EntityId == entityId)
            .Where(a => new[] { SalaryExpenseAccount, WagesExpenseAccount, SocialExpenseAccount, SalaryPayableAccount, SocialPayableAccount }
                .Contains(a.AccountNumber))
            .ToDictionaryAsync(a => a.AccountNumber, a => a.Id, ct);

        if (!accounts.ContainsKey(SalaryExpenseAccount) && !accounts.ContainsKey(WagesExpenseAccount))
            throw new InvalidOperationException(
                $"Salary expense account ({SalaryExpenseAccount} or {WagesExpenseAccount}) not found. Please ensure the chart of accounts is set up.");
        if (!accounts.ContainsKey(SalaryPayableAccount))
            throw new InvalidOperationException(
                $"Salary payable account ({SalaryPayableAccount}) not found. Please ensure the chart of accounts is set up.");

        // Get all active employees with current contracts
        var periodStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);

        var employeesWithContracts = await (
            from e in _db.Employees
            where e.EntityId == entityId
                && e.Status != Domain.Entities.Hr.EmployeeStatus.Terminated
                && e.HireDate <= entryDate
                && (e.TerminationDate == null || e.TerminationDate >= new DateOnly(request.Year, request.Month, 1))
            join c in _db.Contracts on e.Id equals c.EmployeeId
            where c.ValidFrom < periodEnd && (c.ValidTo == null || c.ValidTo >= periodStart)
            select new
            {
                Employee = e,
                Contract = c,
            }
        ).ToListAsync(ct);

        // Pick the latest valid contract per employee
        var latestContracts = employeesWithContracts
            .GroupBy(x => x.Employee.Id)
            .Select(g => g.OrderByDescending(x => x.Contract.ValidFrom).First())
            .ToList();

        if (latestContracts.Count == 0)
            return new PayrollPostingResult { EmployeesProcessed = 0, JournalEntriesCreated = 0 };

        // Get next entry number
        var lastEntryNumber = await _db.JournalEntries
            .Where(je => je.EntityId == entityId)
            .MaxAsync(je => (long?)je.EntryNumber, ct) ?? 0;

        // Resolve employee cost center IDs
        var employeeIds = latestContracts.Select(x => x.Employee.Id).ToList();
        var costCenters = await _db.CostCenters
            .Where(cc => cc.EntityId == entityId && cc.HrEmployeeId != null && employeeIds.Contains(cc.HrEmployeeId.Value))
            .ToDictionaryAsync(cc => cc.HrEmployeeId!.Value, cc => cc.Id, ct);

        var errors = new List<PayrollPostingError>();
        var totalGross = 0m;
        var totalSocial = 0m;
        var entriesCreated = 0;

        foreach (var item in latestContracts)
        {
            var emp = item.Employee;
            var contract = item.Contract;

            try
            {
                var grossCents = contract.GrossAmountCents;
                if (grossCents <= 0) continue;

                var grossAmount = grossCents / 100m;
                var socialAmount = Math.Round(grossAmount * SocialContributionRate, 2);

                // Determine expense account: 4120 for salaried (Gehälter), 4100 for wages (Löhne)
                var expenseAccountNum = contract.SalaryType == Domain.Entities.Hr.SalaryType.Monthly
                    ? SalaryExpenseAccount
                    : WagesExpenseAccount;
                var expenseAccountId = accounts.TryGetValue(expenseAccountNum, out var eid) ? eid
                    : accounts.TryGetValue(SalaryExpenseAccount, out eid) ? eid
                    : accounts[WagesExpenseAccount];

                costCenters.TryGetValue(emp.Id, out var costCenterId);

                var description = $"Gehaltsabrechnung {request.Month:D2}/{request.Year} – {emp.FirstName} {emp.LastName}";

                var entry = JournalEntry.Create(
                    entityId, ++lastEntryNumber, entryDate,
                    description, fiscalPeriod.Id, _currentUser.UserId,
                    sourceType: "payroll",
                    sourceRef: $"{request.Year}-{request.Month:D2}");

                short lineNo = 1;

                // Debit: Salary expense
                entry.AddLine(JournalEntryLine.CreateDebit(
                    lineNo++, expenseAccountId, grossAmount,
                    description: $"{emp.FirstName} {emp.LastName} – Bruttolohn",
                    costCenterId: costCenterId,
                    hrEmployeeId: emp.Id));

                // Debit: Social contributions (employer share)
                if (accounts.ContainsKey(SocialExpenseAccount))
                {
                    entry.AddLine(JournalEntryLine.CreateDebit(
                        lineNo++, accounts[SocialExpenseAccount], socialAmount,
                        description: $"{emp.FirstName} {emp.LastName} – AG-Sozialabgaben",
                        costCenterId: costCenterId,
                        hrEmployeeId: emp.Id));
                }

                // Credit: Salary payable
                entry.AddLine(JournalEntryLine.CreateCredit(
                    lineNo++, accounts[SalaryPayableAccount], grossAmount,
                    description: $"{emp.FirstName} {emp.LastName} – Gehaltsverbindlichkeit",
                    costCenterId: costCenterId,
                    hrEmployeeId: emp.Id));

                // Credit: Social payable
                if (accounts.ContainsKey(SocialExpenseAccount) && accounts.ContainsKey(SocialPayableAccount))
                {
                    entry.AddLine(JournalEntryLine.CreateCredit(
                        lineNo++, accounts[SocialPayableAccount], socialAmount,
                        description: $"{emp.FirstName} {emp.LastName} – Sozialversicherungsverbindlichkeit",
                        costCenterId: costCenterId,
                        hrEmployeeId: emp.Id));
                }
                else if (accounts.ContainsKey(SocialExpenseAccount))
                {
                    // Fallback: credit to main salary payable
                    entry.AddLine(JournalEntryLine.CreateCredit(
                        lineNo++, accounts[SalaryPayableAccount], socialAmount,
                        description: $"{emp.FirstName} {emp.LastName} – Sozialversicherungsverbindlichkeit",
                        costCenterId: costCenterId,
                        hrEmployeeId: emp.Id));
                }

                _db.JournalEntries.Add(entry);
                await _db.SaveChangesAsync(ct);

                totalGross += grossAmount;
                totalSocial += socialAmount;
                entriesCreated++;
            }
            catch (Exception ex)
            {
                errors.Add(new PayrollPostingError($"{emp.FirstName} {emp.LastName}", ex.Message));
            }
        }

        return new PayrollPostingResult
        {
            EmployeesProcessed = latestContracts.Count,
            JournalEntriesCreated = entriesCreated,
            TotalGrossSalary = totalGross,
            TotalSocialContributions = totalSocial,
            Errors = errors,
        };
    }
}
