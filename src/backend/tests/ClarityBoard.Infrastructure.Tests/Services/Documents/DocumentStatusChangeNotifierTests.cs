using ClarityBoard.Application.Common.Interfaces;
using ClarityBoard.Infrastructure.Services.Documents;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClarityBoard.Infrastructure.Tests.Services.Documents;

public class DocumentStatusChangeNotifierTests
{
    [Fact]
    public async Task NotifyAsync_ForwardsStatusChangeToAccountingHubNotifier()
    {
        var fakeNotifier = new FakeAccountingHubNotifier();
        var sut = new DocumentStatusChangeNotifier(fakeNotifier, NullLogger<DocumentStatusChangeNotifier>.Instance);
        var entityId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        await sut.NotifyAsync(entityId, documentId, "review", CancellationToken.None);

        Assert.Equal(entityId, fakeNotifier.EntityId);
        Assert.Equal(documentId, fakeNotifier.DocumentId);
        Assert.Equal("review", fakeNotifier.Status);
    }

    [Fact]
    public async Task NotifyAsync_WhenHubNotifierThrows_DoesNotThrow()
    {
        var fakeNotifier = new FakeAccountingHubNotifier { ThrowOnNotifyDocumentStatusChanged = true };
        var sut = new DocumentStatusChangeNotifier(fakeNotifier, NullLogger<DocumentStatusChangeNotifier>.Instance);

        var exception = await Record.ExceptionAsync(() => sut.NotifyAsync(Guid.NewGuid(), Guid.NewGuid(), "failed", CancellationToken.None));

        Assert.Null(exception);
    }

    private sealed class FakeAccountingHubNotifier : IAccountingHubNotifier
    {
        public bool ThrowOnNotifyDocumentStatusChanged { get; init; }
        public Guid EntityId { get; private set; }
        public Guid DocumentId { get; private set; }
        public string? Status { get; private set; }

        public Task NotifyDocumentStatusChangedAsync(Guid entityId, Guid documentId, string status, CancellationToken ct)
        {
            if (ThrowOnNotifyDocumentStatusChanged)
                throw new InvalidOperationException("boom");

            EntityId = entityId;
            DocumentId = documentId;
            Status = status;
            return Task.CompletedTask;
        }

        public Task NotifyJournalEntryPostedAsync(Guid entityId, Guid entryId, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyJournalEntryCreatedAsync(Guid entityId, Guid entryId, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyDatevExportReadyAsync(Guid entityId, Guid exportId, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyPlanUpdatedAsync(Guid entityId, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyTrialBalanceRefreshedAsync(Guid entityId, CancellationToken ct) => Task.CompletedTask;
    }
}