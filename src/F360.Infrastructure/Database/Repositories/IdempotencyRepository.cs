using F360.Domain.Entities;
using F360.Domain.Interfaces.Repositories;
using F360.Infrastructure.Database.Configuration;
using MongoDB.Driver;

namespace F360.Infrastructure.Database.Repositories;

public class IdempotencyRepository(MongoDbContext context) : IIdempotencyRepository
{
    public async Task<IdempotencyKey?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return await context.IdempotencyRecords.Find(x => x.Key == key).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(IdempotencyKey record, CancellationToken cancellationToken)
    {
        await context.IdempotencyRecords.InsertOneAsync(record, new InsertOneOptions(), cancellationToken);
    }
}
