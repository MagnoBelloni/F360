using F360.Domain.Entities;
using F360.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace F360.Infrastructure.Database.Configuration;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);

        InitializeCollections().Wait();
    }

    public IMongoCollection<Job> Jobs => _database.GetCollection<Job>("jobs");
    public IMongoCollection<OutboxMessage> OutboxMessages => _database.GetCollection<OutboxMessage>("outbox_messages");
    public IMongoCollection<IdempotencyKey> IdempotencyRecords => _database.GetCollection<IdempotencyKey>("idempotency_key");

    private async Task InitializeCollections()
    {
        var collections = await _database.ListCollectionNamesAsync();
        var existingCollections = await collections.ToListAsync();

        if (!existingCollections.Contains("jobs"))
        {
            await _database.CreateCollectionAsync("jobs");
        }

        if (!existingCollections.Contains("outbox_messages"))
        {
            await _database.CreateCollectionAsync("outbox_messages");
        }

        if (!existingCollections.Contains("idempotency_key"))
        {
            await _database.CreateCollectionAsync("idempotency_key");
        }

        await CreateIndexesAsync();
    }

    private async Task CreateIndexesAsync()
    {
        var idempotencyIndexKeys = Builders<IdempotencyKey>.IndexKeys.Ascending(x => x.CreatedAt);
        var idempotencyIndexOptions = new CreateIndexOptions
        {
            ExpireAfter = TimeSpan.FromHours(24)
        };

        await IdempotencyRecords.Indexes.CreateOneAsync(
            new CreateIndexModel<IdempotencyKey>(idempotencyIndexKeys, idempotencyIndexOptions));

        var outboxStatusIndex = Builders<OutboxMessage>.IndexKeys
            .Ascending(x => x.Status)
            .Ascending(x => x.ScheduledTime)
            .Ascending(x => x.LockedUntil);

        await OutboxMessages.Indexes.CreateOneAsync(
            new CreateIndexModel<OutboxMessage>(outboxStatusIndex));

        var idempotencyKeyUniqueIndex = Builders<IdempotencyKey>.IndexKeys
            .Ascending(x => x.Key);

        await IdempotencyRecords.Indexes.CreateOneAsync(
            new CreateIndexModel<IdempotencyKey>(idempotencyKeyUniqueIndex, new CreateIndexOptions { Unique = true }));
    }
}
