using F360.Domain.Entities;
using F360.Domain.Interfaces.Repositories;
using F360.Infrastructure.Database.Configuration;
using MongoDB.Driver;

namespace F360.Infrastructure.Database.Repositories;

public class JobRepository(MongoDbContext context) : IJobRepository
{
    public async Task<Job> CreateAsync(Job job, CancellationToken cancellationToken)
    {
        await context.Jobs.InsertOneAsync(job, new InsertOneOptions(), cancellationToken);
        return job;
    }

    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Jobs.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(Job job, CancellationToken cancellationToken)
    {
        await context.Jobs.ReplaceOneAsync(x => x.Id == job.Id, job, cancellationToken: cancellationToken);
    }
}
