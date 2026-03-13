using ClarityBoard.Application.Common.Interfaces;
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
    private readonly IDatevExportService _datevExportService;

    public AccountingController(ISender mediator, IDatevExportService datevExportService)
    {
        _mediator = mediator;
        _datevExportService = datevExportService;
    }

    // ── Accounts ─────────────────────────────────────────────────────────

    [HttpGet("accounts")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(
        [FromQuery] string? accountType,
        [FromQuery] int? accountClass = null,
        [FromQuery] string? search = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAccountsQuery
        {
            AccountType = accountType,
            AccountClass = accountClass,
            Search = search,
            ActiveOnly = activeOnly,
        }, ct);
        return Ok(result);
    }

    [HttpGet("accounts/{id:guid}")]
    [ProducesResponseType(typeof(AccountDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDetailDto>> GetAccountDetail(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAccountDetailQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("accounts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateAccount(
        CreateAccountCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAccountDetail), new { id }, id);
    }

    [HttpPut("accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccount(
        Guid id, UpdateAccountCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest("Route id does not match body id.");

        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("accounts/batch-translate")]
    [ProducesResponseType(typeof(BatchTranslateResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BatchTranslateResult>> BatchTranslateAccountNames(CancellationToken ct)
    {
        var result = await _mediator.Send(new BatchTranslateAccountNamesCommand(), ct);
        return Ok(result);
    }

    [HttpPost("accounts/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateAccount(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateAccountCommand(id), ct);
        return NoContent();
    }

    [HttpPost("accounts/seed")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> SeedChartOfAccounts(CancellationToken ct)
    {
        var count = await _mediator.Send(new SeedChartOfAccountsCommand(), ct);
        return Ok(count);
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

    [HttpPut("journal-entries/{id:guid}")]
    [ProducesResponseType(typeof(JournalEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JournalEntryDto>> UpdateJournalEntry(
        [FromQuery] Guid entityId, Guid id, [FromBody] UpdateJournalEntryRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateJournalEntryCommand
        {
            Id = id,
            EntityId = entityId,
            EntryDate = request.EntryDate,
            Description = request.Description,
            Lines = request.Lines,
        }, ct);
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

    // ── Post Journal Entry (GoBD hash chaining) ──────────────────────────

    [HttpPost("journal-entries/{id:guid}/post")]
    [ProducesResponseType(typeof(PostJournalEntryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostJournalEntryResult>> PostJournalEntry(
        Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new PostJournalEntryCommand { JournalEntryId = id }, ct);
        return Ok(result);
    }

    // ── Cost Centers ─────────────────────────────────────────────────────

    [HttpGet("cost-centers")]
    [ProducesResponseType(typeof(List<CostCenterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CostCenterDto>>> GetCostCenters(
        [FromQuery] Guid entityId, [FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCostCentersQuery { EntityId = entityId, ActiveOnly = activeOnly }, ct);
        return Ok(result);
    }

    [HttpPost("cost-centers")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateCostCenter(
        CreateCostCenterCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetCostCenters), new { }, id);
    }

    // ── DATEV Exports ────────────────────────────────────────────────────

    [HttpGet("datev/exports")]
    [ProducesResponseType(typeof(List<DatevExportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DatevExportDto>>> GetDatevExports(
        [FromQuery] Guid entityId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDatevExportsQuery { EntityId = entityId }, ct);
        return Ok(result);
    }

    [HttpPost("datev/exports")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> GenerateDatevExport(
        GenerateDatevExportCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return AcceptedAtAction(nameof(GetDatevExports), new { }, id);
    }

    [HttpGet("datev/exports/{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDatevExport(Guid id, CancellationToken ct = default)
    {
        var stream = await _datevExportService.GetExportStreamAsync(id, ct);
        return File(stream, "text/csv", $"datev-export-{id:N}.csv");
    }

    // ── Accounting Scenarios ─────────────────────────────────────────────

    [HttpGet("scenarios")]
    [ProducesResponseType(typeof(List<AccountingScenarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AccountingScenarioDto>>> GetAccountingScenarios(
        [FromQuery] Guid entityId, [FromQuery] int? year = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAccountingScenariosQuery { EntityId = entityId, Year = year }, ct);
        return Ok(result);
    }

    [HttpPost("scenarios")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateAccountingScenario(
        CreateAccountingScenarioCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAccountingScenarios), new { }, id);
    }

    // ── Business Partners ────────────────────────────────────────────────

    [HttpGet("business-partners")]
    [ProducesResponseType(typeof(PagedResult<BusinessPartnerListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BusinessPartnerListItemDto>>> GetBusinessPartners(
        [FromQuery] Guid entityId,
        [FromQuery] bool? isCreditor = null,
        [FromQuery] bool? isDebtor = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetBusinessPartnersQuery(entityId, isCreditor, isDebtor, isActive, search, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("business-partners/{id:guid}")]
    [ProducesResponseType(typeof(BusinessPartnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessPartnerDto>> GetBusinessPartner(
        [FromQuery] Guid entityId, Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBusinessPartnerQuery(entityId, id), ct);
        return Ok(result);
    }

    [HttpGet("business-partners/search")]
    [ProducesResponseType(typeof(List<BusinessPartnerSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BusinessPartnerSearchResultDto>>> SearchBusinessPartners(
        [FromQuery] Guid entityId, [FromQuery] string q = "", CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchBusinessPartnersQuery(entityId, q), ct);
        return Ok(result);
    }

    [HttpPost("business-partners")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateBusinessPartner(
        CreateBusinessPartnerCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetBusinessPartner), new { id }, id);
    }

    [HttpPut("business-partners/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBusinessPartner(
        Guid id, UpdateBusinessPartnerCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest("Route id does not match body id.");

        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("business-partners/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateBusinessPartner(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateBusinessPartnerCommand(id), ct);
        return NoContent();
    }

    [HttpPost("documents/{id:guid}/assign-partner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignDocumentPartner(
        Guid id, [FromBody] AssignDocumentPartnerRequest request, CancellationToken ct)
    {
        await _mediator.Send(new AssignDocumentPartnerCommand(id, request.BusinessPartnerId), ct);
        return NoContent();
    }

    [HttpPost("documents/{id:guid}/confirm-partner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmPartnerMatch(
        Guid id, [FromBody] ConfirmPartnerMatchRequest request, CancellationToken ct)
    {
        await _mediator.Send(new ConfirmPartnerMatchCommand(id, request.BusinessPartnerId), ct);
        return NoContent();
    }

    // ── Travel Cost Sync ─────────────────────────────────────────────────

    [HttpPost("travel-costs/sync")]
    [ProducesResponseType(typeof(SyncTravelCostsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SyncTravelCostsResult>> SyncTravelCosts(
        SyncTravelCostsCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}

// ── Request Models ───────────────────────────────────────────────────────

public record ReverseJournalEntryRequest
{
    public required string Reason { get; init; }
}

public record UpdateJournalEntryRequest
{
    public required DateOnly EntryDate { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<CreateJournalEntryLineRequest> Lines { get; init; }
}

public record AssignDocumentPartnerRequest
{
    public Guid? BusinessPartnerId { get; init; }
}

public record ConfirmPartnerMatchRequest
{
    public required Guid BusinessPartnerId { get; init; }
}
