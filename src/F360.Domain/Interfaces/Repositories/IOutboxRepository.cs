using F360.Domain.Entities;

namespace F360.Domain.Interfaces.Repositories;

public interface IOutboxRepository
{
    Task CreateAsync(OutboxMessage message, CancellationToken cancellationToken);
    Task<OutboxMessage?> GetAndLockNextPendingMessageAsync(CancellationToken cancellationToken);
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken);
}
