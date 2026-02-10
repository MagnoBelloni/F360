using F360.Domain.Dtos.Responses;

namespace F360.Domain.Interfaces.UseCases;

public interface IGetJobUseCase
{
    Task<JobResponse> ExecuteAsync(Guid jobId, CancellationToken cancellationToken);
}
