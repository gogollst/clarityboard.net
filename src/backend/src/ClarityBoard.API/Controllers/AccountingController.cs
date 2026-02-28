using ClarityBoard.Application.Common.Models;
using ClarityBoard.Application.Features.Accounting.Commands;
using ClarityBoard.Application.Features.Accounting.DTOs;
using ClarityBoard.Application.Features.Accounting.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClarityBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AccountingController : ControllerBase
{
    private readonly ISender _mediator;

    public AccountingController(ISender mediator)
    {
        _mediator = mediator;
    }

    // ── Accounts ─────────────────────────────────────────────────────────

    [HttpGet("accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(
        [FromQuery] string? accountType, [FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAccountsQuery
        {
            AccountType = accountType,
            ActiveOnly = activeOnly,
        }, ct);
        return Ok(result);
    }

    // ── Trial Balance ────────────────────────────────────────────────────

    [HttpGet("trial-balance")]
    [ProducesResponseType(typeof(TrialBalanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrialBalanceDto>> GetTrialBalance(
        [FromQuery] Guid entityId, [FromQuery] short year, [FromQuery] short month, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTrialBalanceQuery(entityId, year, month), ct);
        return Ok(result);
    }

    // ── Profit & Loss (GuV nach HGB Paragraph 275) ────────────────────────────

    [HttpGet("pnl")]
    [ProducesResponseType(typeof(ProfitAndLossDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfitAndLossDto>> GetProfitAndLoss(
        [FromQuery] Guid entityId,
        [FromQuery] short year,
        [FromQuery] short month,
        [FromQuery] short? compareYear = null,
        [FromQuery] short? compareMonth = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetProfitAndLossQuery(entityId, year, month, compareYear, compareMonth), ct);
        return Ok(result);
    }

    // ── Balance Sheet (Bilanz nach HGB Paragraph 266) ─────────────────────────

    [HttpGet("balance-sheet")]
    [ProducesResponseType(typeof(BalanceSheetDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceSheetDto>> GetBalanceSheet(
        [FromQuery] Guid entityId,
        [FromQuery] short year,
        [FromQuery] short month,
        [FromQuery] short? compareYear = null,
        [FromQuery] short? compareMonth = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetBalanceSheetQuery(entityId, year, month, compareYear, compareMonth), ct);
        return Ok(result);
    }

    // ── VAT Reconciliation (UStVA Preparation) ─────────────────────────

    [HttpGet("vat-reconciliation")]
    [ProducesResponseType(typeof(VatReconciliationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VatReconciliationDto>> GetVatReconciliation(
        [FromQuery] Guid entityId,
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetVatReconciliationQuery(entityId, periodStart, periodEnd), ct);
        return Ok(result);
    }

    // ── Journal Entries ──────────────────────────────────────────────────

    [HttpPost("journal-entries")]
    [ProducesResponseType(typeof(JournalEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JournalEntryDto>> CreateJournalEntry(
        CreateJournalEntryCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetJournalEntryDetail), new { entityId = result.Id, id = result.Id }, result);
    }

    [HttpGet("journal-entries")]
    [ProducesResponseType(typeof(PagedResult<JournalEntryListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<JournalEntryListDto>>> GetJournalEntries(
        [FromQuery] Guid entityId,
        [FromQuery] short? year = null,
        [FromQuery] short? month = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetJournalEntriesQuery(entityId, year, month, status, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("journal-entries/{id:guid}")]
    [ProducesResponseType(typeof(JournalEntryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JournalEntryDetailDto>> GetJournalEntryDetail(
        [FromQuery] Guid entityId, Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetJournalEntryDetailQuery(entityId, id), ct);
        return Ok(result);
    }

    [HttpPost("journal-entries/{id:guid}/reverse")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> ReverseJournalEntry(
        [FromQuery] Guid entityId, Guid id, [FromBody] ReverseJournalEntryRequest request, CancellationToken ct = default)
    {
        var reversalId = await _mediator.Send(
            new ReverseJournalEntryCommand(entityId, id, request.Reason), ct);
        return Ok(reversalId);
    }

    // ── Fiscal Periods ───────────────────────────────────────────────────

    [HttpGet("fiscal-periods")]
    [ProducesResponseType(typeof(List<FiscalPeriodDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FiscalPeriodDto>>> GetFiscalPeriods(
        [FromQuery] Guid entityId, [FromQuery] short? year = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFiscalPeriodsQuery(entityId, year), ct);
        return Ok(result);
    }

    [HttpPost("fiscal-periods/{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseFiscalPeriod(
        [FromQuery] Guid entityId, Guid id, [FromQuery] bool hardClose = false, CancellationToken ct = default)
    {
        await _mediator.Send(new CloseFiscalPeriodCommand(entityId, id, hardClose), ct);
        return NoContent();
    }

    [HttpPost("fiscal-periods/{id:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReopenFiscalPeriod(
        [FromQuery] Guid entityId, Guid id, CancellationToken ct = default)
    {
        await _mediator.Send(new ReopenFiscalPeriodCommand(entityId, id), ct);
        return NoContent();
    }
}

// ── Request Models ───────────────────────────────────────────────────────

public record ReverseJournalEntryRequest
{
    public required string Reason { get; init; }
}
