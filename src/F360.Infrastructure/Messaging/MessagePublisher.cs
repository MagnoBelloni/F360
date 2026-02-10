using F360.Domain.Dtos.Messages;
using F360.Domain.Interfaces;
using MassTransit;

namespace F360.Infrastructure.Messaging;

public class MessagePublisher(IPublishEndpoint publishEndpoint) : IMessagePublisher
{
    public async Task PublishAsync(JobMessage message)
    {
        await publishEndpoint.Publish(message);
    }
}
