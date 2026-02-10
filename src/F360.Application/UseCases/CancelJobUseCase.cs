using F360.Domain.Enums;
using F360.Domain.Exceptions;
using F360.Domain.Interfaces.Repositories;
using F360.Domain.Interfaces.UseCases;

namespace F360.Application.UseCases;

public class CancelJobUseCase(IJobRepository jobRepository) : ICancelJobUseCase
{
    public async Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new NotFoundException("Job not found");

        if (job.Status != JobStatus.Pending)
        {
            throw new BusinessException("Only pending jobs can be cancelled");
        }

        job.Status = JobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;

        await jobRepository.UpdateAsync(job, cancellationToken);
    }
}
