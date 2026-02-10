using F360.Application.DTOs.Requests;
using F360.Domain.Dtos.Messages;
using F360.Domain.Dtos.Responses;
using F360.Domain.Entities;
using F360.Domain.Enums;
using F360.Domain.Exceptions;
using F360.Domain.Interfaces.Repositories;
using F360.Domain.Interfaces.UseCases;
using System.Text.Json;

namespace F360.Application.UseCases;

public class CreateJobUseCase(
    IJobRepository jobRepository,
    IOutboxRepository outboxRepository,
    IIdempotencyRepository idempotencyRepository) : ICreateJobUseCase
{
    public async Task<CreateJobResponse> ExecuteAsync(CreateJobRequest request, string idempotencyKey, CancellationToken cancellationToken)
    {
        var existingRecord = await idempotencyRepository.GetByKeyAsync(idempotencyKey, cancellationToken);
        if (existingRecord != null)
        {
            throw new ConflictException("Duplicate request detected");
        }

        var job = new Job
        {
            Id = Guid.NewGuid(),
            Cep = request.Cep,
            Priority = request.Priority,
            Status = JobStatus.Pending,
            ScheduledTime = request.ScheduledTime,
            CreatedAt = DateTime.UtcNow
        };

        await jobRepository.CreateAsync(job, cancellationToken);

        var idempotencyRecord = new IdempotencyKey
        {
            Key = idempotencyKey,
            JobId = job.Id,
            CreatedAt = DateTime.UtcNow
        };

        await idempotencyRepository.CreateAsync(idempotencyRecord, cancellationToken);

        var message = new JobMessage
        {
            JobId = job.Id,
            Cep = job.Cep,
            Priority = job.Priority
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            Payload = JsonSerializer.Serialize(message),
            Priority = job.Priority,
            Status = OutboxStatus.Pending,
            ScheduledTime = job.ScheduledTime ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        await outboxRepository.CreateAsync(outboxMessage, cancellationToken);

        return new CreateJobResponse { Id = job.Id };
    }
}
