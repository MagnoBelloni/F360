using F360.Domain.Entities;
using F360.Domain.Enums;
using F360.Domain.Interfaces.Repositories;
using F360.Infrastructure.Database.Configuration;
using MongoDB.Driver;

namespace F360.Infrastructure.Database.Repositories;

public class OutboxRepository(MongoDbContext context) : IOutboxRepository
{
    public async Task CreateAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await context.OutboxMessages.InsertOneAsync(message, new InsertOneOptions(), cancellationToken);
    }

    public async Task<OutboxMessage?> GetAndLockNextPendingMessageAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var lockUntil = now.AddMinutes(5);

        var filter = Builders<OutboxMessage>.Filter.And(
            Builders<OutboxMessage>.Filter.Eq(x => x.Status, OutboxStatus.Pending),
            Builders<OutboxMessage>.Filter.Lte(x => x.ScheduledTime, now),
            Builders<OutboxMessage>.Filter.Or(
                Builders<OutboxMessage>.Filter.Eq(x => x.LockedUntil, null),
                Builders<OutboxMessage>.Filter.Lt(x => x.LockedUntil, now)
            )
        );

        var update = Builders<OutboxMessage>.Update.Set(x => x.LockedUntil, lockUntil);

        var options = new FindOneAndUpdateOptions<OutboxMessage>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await context.OutboxMessages.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
    }

    public async Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await context.OutboxMessages.ReplaceOneAsync(x => x.Id == message.Id, message, cancellationToken: cancellationToken);
    }
}
