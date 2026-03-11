namespace ClarityBoard.Application.Common.Interfaces;

public interface IAccountingHubNotifier
{
    Task NotifyJournalEntryPostedAsync(Guid entityId, Guid entryId, CancellationToken ct);
    Task NotifyJournalEntryCreatedAsync(Guid entityId, Guid entryId, CancellationToken ct);
    Task NotifyDocumentStatusChangedAsync(Guid entityId, Guid documentId, string status, CancellationToken ct);
    Task NotifyDatevExportReadyAsync(Guid entityId, Guid exportId, CancellationToken ct);
    Task NotifyPlanUpdatedAsync(Guid entityId, CancellationToken ct);
    Task NotifyTrialBalanceRefreshedAsync(Guid entityId, CancellationToken ct);
}
