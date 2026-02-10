using F360.Domain.Dtos.Messages;

namespace F360.Domain.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(JobMessage message);
}
