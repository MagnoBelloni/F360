using F360.Domain.Entities;

namespace F360.Domain.Interfaces.Repositories;

public interface IIdempotencyRepository
{
    Task<IdempotencyKey?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task CreateAsync(IdempotencyKey record, CancellationToken cancellationToken);
}
