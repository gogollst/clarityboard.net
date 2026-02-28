namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// Abstraction for pushing alert events to connected clients in real-time.
/// Implemented via SignalR in the API layer.
/// </summary>
public interface IAlertNotificationService
{
    Task SendAlertAsync(
        Guid entityId,
        object alertPayload,
        CancellationToken ct = default);
}
