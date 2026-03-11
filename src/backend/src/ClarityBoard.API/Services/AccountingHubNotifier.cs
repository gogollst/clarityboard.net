using ClarityBoard.API.Hubs;
using ClarityBoard.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ClarityBoard.API.Services;

/// <summary>
/// Pushes real-time accounting events to connected clients via SignalR AccountingHub.
/// </summary>
public class AccountingHubNotifier : IAccountingHubNotifier
{
    private readonly IHubContext<AccountingHub> _hub;

    public AccountingHubNotifier(IHubContext<AccountingHub> hub) => _hub = hub;

    public Task NotifyJournalEntryPostedAsync(Guid entityId, Guid entryId, CancellationToken ct)
        => _hub.Clients.Group($"accounting:{entityId}")
               .SendAsync("JournalEntryPosted", new { EntryId = entryId }, ct);

    public Task NotifyJournalEntryCreatedAsync(Guid entityId, Guid entryId, CancellationToken ct)
        => _hub.Clients.Group($"accounting:{entityId}")
               .SendAsync("JournalEntryCreated", new { EntryId = entryId }, ct);

    public Task NotifyDocumentStatusChangedAsync(Guid entityId, Guid documentId, string status, CancellationToken ct)
        => _hub.Clients.Group($"accounting:{entityId}")
               .SendAsync("DocumentStatusChanged", new { DocumentId = documentId, Status = status }, ct);

    public Task NotifyDatevExportReadyAsync(Guid entityId, Guid exportId, CancellationToken ct)
        => _hub.Clients.Group($"accounting:{entityId}")
               .SendAsync("DatevExportReady", new { ExportId = exportId }, ct);

    public Task NotifyPlanUpdatedAsync(Guid entityId, CancellationToken ct)
        => _hub.Clients.Group($"accounting:{entityId}")
               .SendAsync("PlanUpdated", new { EntityId = entityId }, ct);

    public Task NotifyTrialBalanceRefreshedAsync(Guid entityId, CancellationToken ct)
        => _hub.Clients.Group($"accounting:{entityId}")
               .SendAsync("TrialBalanceRefreshed", new { EntityId = entityId }, ct);
}
