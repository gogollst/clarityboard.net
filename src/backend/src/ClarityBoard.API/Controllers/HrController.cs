using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.Hr.Commands;
using ClarityBoard.Application.Features.Hr.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/hr")]
public class HrController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IHrExportService _hrExport;
    private readonly ICurrentUser _currentUser;

    public HrController(ISender mediator, IHrExportService hrExport, ICurrentUser currentUser)
    {
        _mediator    = mediator;
        _hrExport    = hrExport;
        _currentUser = currentUser;
    }

    // ── Employees ──

    [HttpGet("employees")]
    [ProducesResponseType(typeof(PagedResult<EmployeeListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeListDto>>> ListEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? employeeType = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? entityId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListEmployeesQuery
        {
            Page         = page,
            PageSize     = pageSize,
            Search       = search,
            Status       = status,
            EmployeeType = employeeType,
            DepartmentId = departmentId,
            EntityId     = entityId,
        }, ct);
        return Ok(result);
    }

    [HttpPost("employees")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateEmployee(
        [FromBody] CreateEmployeeCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetEmployee), new { id }, new { id });
    }

    [HttpGet("employees/{id:guid}")]
    [ProducesResponseType(typeof(EmployeeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDetailDto>> GetEmployee(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEmployeeQuery(id), ct);
        return Ok(result);
    }

    [HttpPut("employees/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmployee(
        Guid id, [FromBody] UpdateEmployeeRequest body, CancellationToken ct)
    {
        await _mediator.Send(new UpdateEmployeeCommand
        {
            Id          = id,
            FirstName   = body.FirstName,
            LastName    = body.LastName,
            DateOfBirth = body.DateOfBirth,
            TaxId       = body.TaxId,
            ManagerId   = body.ManagerId,
            DepartmentId = body.DepartmentId,
        }, ct);
        return NoContent();
    }

    [HttpPost("employees/{id:guid}/terminate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TerminateEmployee(
        Guid id, [FromBody] TerminateEmployeeRequest body, CancellationToken ct)
    {
        await _mediator.Send(new TerminateEmployeeCommand
        {
            EmployeeId      = id,
            TerminationDate = body.TerminationDate,
            Reason          = body.Reason,
        }, ct);
        return NoContent();
    }

    // ── Salary ──

    [HttpGet("employees/{id:guid}/salary-history")]
    [ProducesResponseType(typeof(List<SalaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<SalaryDto>>> GetSalaryHistory(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSalaryHistoryQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("employees/{id:guid}/salary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSalary(
        Guid id, [FromBody] UpdateSalaryRequest body, CancellationToken ct)
    {
        await _mediator.Send(new UpdateSalaryCommand
        {
            EmployeeId         = id,
            GrossAmountCents   = body.GrossAmountCents,
            CurrencyCode       = body.CurrencyCode,
            BonusAmountCents   = body.BonusAmountCents,
            BonusCurrencyCode  = body.BonusCurrencyCode,
            SalaryType         = body.SalaryType,
            PaymentCycleMonths = body.PaymentCycleMonths,
            ValidFrom          = body.ValidFrom,
            ChangeReason       = body.ChangeReason,
        }, ct);
        return NoContent();
    }

    // ── Contracts ──

    [HttpGet("employees/{id:guid}/contracts")]
    [ProducesResponseType(typeof(List<ContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ContractDto>>> GetContractHistory(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetContractHistoryQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("employees/{id:guid}/contracts")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateContract(
        Guid id, [FromBody] CreateContractRequest body, CancellationToken ct)
    {
        await _mediator.Send(new CreateContractCommand
        {
            EmployeeId       = id,
            ContractType     = body.ContractType,
            WeeklyHours      = body.WeeklyHours,
            WorkdaysPerWeek  = body.WorkdaysPerWeek,
            StartDate        = body.StartDate,
            EndDate          = body.EndDate,
            ProbationEndDate = body.ProbationEndDate,
            NoticeWeeks      = body.NoticeWeeks,
            ValidFrom        = body.ValidFrom,
            ChangeReason     = body.ChangeReason,
        }, ct);
        return NoContent();
    }

    // ── Departments ──

    [HttpGet("departments")]
    [ProducesResponseType(typeof(List<DepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<DepartmentDto>>> ListDepartments(
        [FromQuery] Guid? entityId, CancellationToken ct)
    {
        if (entityId is null || entityId == Guid.Empty)
            return BadRequest("entityId is required.");
        var result = await _mediator.Send(new ListDepartmentsQuery(entityId.Value), ct);
        return Ok(result);
    }

    [HttpPost("departments")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateDepartment(
        [FromBody] CreateDepartmentCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return Created(string.Empty, new { id });
    }

    // ── Leave Types ──

    [HttpGet("leave-types")]
    [ProducesResponseType(typeof(List<LeaveTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<LeaveTypeDto>>> ListLeaveTypes(
        [FromQuery] Guid? entityId, CancellationToken ct)
    {
        if (entityId is null || entityId == Guid.Empty)
            return BadRequest("entityId is required.");
        var result = await _mediator.Send(new ListLeaveTypesQuery(entityId.Value), ct);
        return Ok(result);
    }

    [HttpPost("leave-types")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateLeaveType(
        [FromBody] CreateLeaveTypeCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return Created(string.Empty, new { id });
    }

    // ── Leave Requests ──

    [HttpGet("leave-requests")]
    [ProducesResponseType(typeof(PagedResult<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LeaveRequestDto>>> ListLeaveRequests(
        [FromQuery] Guid? entityId,    // Fix 9: optional entity scope
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListLeaveRequestsQuery
        {
            EntityId   = entityId,
            EmployeeId = employeeId,
            Status     = status,
            Year       = year,
            Page       = page,
            PageSize   = pageSize,
        }, ct);
        return Ok(result);
    }

    [HttpPost("leave-requests")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SubmitLeaveRequest(
        [FromBody] SubmitLeaveRequestCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return Created(string.Empty, new { id });
    }

    [HttpPut("leave-requests/{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveLeaveRequest(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ApproveLeaveRequestCommand { LeaveRequestId = id }, ct);
        return NoContent();
    }

    [HttpPut("leave-requests/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectLeaveRequest(
        Guid id, [FromBody] RejectLeaveRequestRequest body, CancellationToken ct)
    {
        await _mediator.Send(new RejectLeaveRequestCommand
        {
            LeaveRequestId = id,
            Reason         = body.Reason,
        }, ct);
        return NoContent();
    }

    // ── Leave Balances ──

    [HttpGet("leave-balances/{employeeId:guid}")]
    [ProducesResponseType(typeof(List<LeaveBalanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LeaveBalanceDto>>> GetLeaveBalance(
        Guid employeeId, [FromQuery] int? year, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLeaveBalanceQuery(employeeId, year), ct);
        return Ok(result);
    }

    // ── Travel Expenses ──

    // NOTE: /datev-export must be declared before /{id} to prevent "datev-export"
    // from being treated as a guid route parameter (ASP.NET Core attribute routing
    // resolves literal segments before wildcard segments automatically, but explicit
    // ordering here documents the intent clearly).

    [HttpGet("travel-expenses/datev-export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportTravelExpensesDatev(
        [FromQuery] Guid entityId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        if (entityId == Guid.Empty)
            return BadRequest("entityId is required.");

        if (!_currentUser.HasPermission("hr.export"))
            return Forbid();

        var bytes    = await _hrExport.ExportTravelExpensesCsvAsync(entityId, from, to, ct);
        var fileName = $"datev-travel-{entityId}-{from:yyyy-MM-dd}-{to:yyyy-MM-dd}.csv";
        return File(bytes, "text/csv", fileName);
    }

    [HttpGet("travel-expenses")]
    [ProducesResponseType(typeof(PagedResult<TravelExpenseReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TravelExpenseReportDto>>> ListTravelExpenses(
        [FromQuery] Guid? entityId,
        [FromQuery] Guid? employeeId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListTravelExpensesQuery
        {
            EntityId   = entityId,
            EmployeeId = employeeId,
            Status     = status,
            Page       = page,
            PageSize   = pageSize,
        }, ct);
        return Ok(result);
    }

    [HttpPost("travel-expenses")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateTravelExpenseReport(
        [FromBody] CreateTravelExpenseReportRequest body, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateTravelExpenseReportCommand
        {
            EmployeeId      = body.EmployeeId,
            Title           = body.Title,
            TripStartDate   = body.TripStartDate,
            TripEndDate     = body.TripEndDate,
            Destination     = body.Destination,
            BusinessPurpose = body.BusinessPurpose,
        }, ct);
        return Created(string.Empty, new { id });
    }

    [HttpGet("travel-expenses/{id:guid}")]
    [ProducesResponseType(typeof(TravelExpenseReportDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelExpenseReportDetailDto>> GetTravelExpense(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTravelExpenseQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("travel-expenses/{id:guid}/items")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddTravelExpenseItem(
        Guid id, [FromBody] AddTravelExpenseItemRequest body, CancellationToken ct)
    {
        var itemId = await _mediator.Send(new AddTravelExpenseItemCommand
        {
            ReportId             = id,
            ExpenseType          = body.ExpenseType,
            ExpenseDate          = body.ExpenseDate,
            Description          = body.Description,
            OriginalAmountCents  = body.OriginalAmountCents,
            OriginalCurrencyCode = body.OriginalCurrencyCode,
            ExchangeRate         = body.ExchangeRate,
            ExchangeRateDate     = body.ExchangeRateDate,
            VatRatePercent       = body.VatRatePercent,
            IsDeductible         = body.IsDeductible,
        }, ct);
        return Created(string.Empty, new { id = itemId });
    }

    [HttpPost("travel-expenses/{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitTravelExpense(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new SubmitTravelExpenseCommand { ReportId = id }, ct);
        return NoContent();
    }

    [HttpPut("travel-expenses/{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveTravelExpense(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ApproveTravelExpenseCommand { ReportId = id }, ct);
        return NoContent();
    }

    // ── Work Time ──

    [HttpGet("work-time/{employeeId:guid}")]
    [ProducesResponseType(typeof(PagedResult<WorkTimeEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WorkTimeEntryDto>>> GetWorkTime(
        Guid employeeId,
        [FromQuery] string? month,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetWorkTimeQuery
        {
            EmployeeId = employeeId,
            Month      = month,
            Page       = page,
            PageSize   = pageSize,
        }, ct);
        return Ok(result);
    }

    [HttpPost("work-time")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> LogWorkTime(
        [FromBody] LogWorkTimeCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return Created(string.Empty, new { id });
    }
}

// ── Request DTOs ──

public record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string TaxId,
    Guid? ManagerId,
    Guid? DepartmentId);

public record TerminateEmployeeRequest(
    DateOnly TerminationDate,
    string Reason);

public record UpdateSalaryRequest(
    int GrossAmountCents,
    string CurrencyCode,
    int BonusAmountCents,
    string BonusCurrencyCode,
    string SalaryType,
    int PaymentCycleMonths,
    DateTime ValidFrom,
    string ChangeReason);

public record CreateContractRequest(
    string ContractType,
    decimal WeeklyHours,
    int WorkdaysPerWeek,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateOnly? ProbationEndDate,
    int NoticeWeeks,
    DateTime ValidFrom,
    string ChangeReason);

public record RejectLeaveRequestRequest(string Reason);

public record CreateTravelExpenseReportRequest(
    Guid EmployeeId,
    string Title,
    DateOnly TripStartDate,
    DateOnly TripEndDate,
    string Destination,
    string BusinessPurpose);

public record AddTravelExpenseItemRequest(
    string ExpenseType,
    DateOnly ExpenseDate,
    string Description,
    int OriginalAmountCents,
    string OriginalCurrencyCode,
    decimal ExchangeRate,
    DateOnly ExchangeRateDate,
    decimal? VatRatePercent,
    bool IsDeductible);
