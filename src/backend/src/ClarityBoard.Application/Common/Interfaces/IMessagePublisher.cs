namespace ClarityBoard.Application.Common.Interfaces;

/// <summary>
/// Abstraction for publishing messages to the message bus.
/// Implemented by Infrastructure layer using MassTransit.
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
