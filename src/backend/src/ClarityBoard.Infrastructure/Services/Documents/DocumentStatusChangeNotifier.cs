using ClarityBoard.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Services.Documents;

public sealed class DocumentStatusChangeNotifier
{
    private readonly IAccountingHubNotifier _accountingHubNotifier;
    private readonly ILogger<DocumentStatusChangeNotifier> _logger;

    public DocumentStatusChangeNotifier(
        IAccountingHubNotifier accountingHubNotifier,
        ILogger<DocumentStatusChangeNotifier> logger)
    {
        _accountingHubNotifier = accountingHubNotifier;
        _logger = logger;
    }

    public async Task NotifyAsync(Guid entityId, Guid documentId, string status, CancellationToken ct)
    {
        try
        {
            await _accountingHubNotifier.NotifyDocumentStatusChangedAsync(entityId, documentId, status, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to notify document status change for document {DocumentId} on entity {EntityId} with status {Status}",
                documentId, entityId, status);
        }
    }
}