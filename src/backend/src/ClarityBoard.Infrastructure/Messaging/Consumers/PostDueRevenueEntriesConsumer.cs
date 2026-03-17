using ClarityBoard.Application.Common.Messaging;
using ClarityBoard.Application.Features.Accounting.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClarityBoard.Infrastructure.Messaging.Consumers;

public class PostDueRevenueEntriesConsumer : IConsumer<PostDueRevenueEntries>
{
    private readonly ISender _mediator;
    private readonly ILogger<PostDueRevenueEntriesConsumer> _logger;

    public PostDueRevenueEntriesConsumer(ISender mediator, ILogger<PostDueRevenueEntriesConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PostDueRevenueEntries> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "PostDueRevenueEntries: entity={EntityId}, upToMonth={UpToMonth}",
            msg.EntityId, msg.UpToMonth);

        var result = await _mediator.Send(
            new PostAllDueRevenueEntriesCommand(msg.EntityId, msg.UserId, msg.UpToMonth),
            context.CancellationToken);

        _logger.LogInformation(
            "PostDueRevenueEntries completed: {PostedCount} entries posted for entity {EntityId}",
            result.PostedCount, msg.EntityId);
    }
}
