using F360.Domain.Dtos.Responses;
using F360.Domain.Exceptions;
using F360.Domain.Interfaces.Repositories;
using F360.Domain.Interfaces.UseCases;

namespace F360.Application.UseCases;

public class GetJobUseCase(IJobRepository jobRepository) : IGetJobUseCase
{
    public async Task<JobResponse> ExecuteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new NotFoundException("Job not found");

        return new JobResponse
        {
            Id = job.Id,
            Cep = job.Cep,
            Priority = job.Priority,
            Status = job.Status,
            ScheduledTime = job.ScheduledTime,
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt
        };
    }
}
