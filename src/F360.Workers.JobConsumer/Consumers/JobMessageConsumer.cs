using F360.Domain.Dtos.Messages;
using F360.Domain.Enums;
using F360.Domain.Interfaces.Database.Repositories;
using F360.Domain.Interfaces.HttpClients;
using MassTransit;

namespace F360.Workers.JobConsumer.Consumers;

public class JobMessageConsumer(
    ILogger<JobMessageConsumer> logger,
    IJobRepository jobRepository,
    IViaCepClient viaCepClient) : IConsumer<JobMessage>
{
    public async Task Consume(ConsumeContext<JobMessage> context)
    {
        var message = context.Message;
        
        logger.LogInformation("Processing job {JobId} with CEP {Cep}", message.JobId, message.Cep);

        try
        {
            var job = await jobRepository.GetByIdAsync(message.JobId, context.CancellationToken);
            if (job == null)
            {
                logger.LogWarning("Job {JobId} not found", message.JobId);
                return;
            }

            if (job.Status == JobStatus.Cancelled)
            {
                logger.LogInformation("Job {JobId} was cancelled, skipping processing", message.JobId);
                return;
            }

            job.Status = JobStatus.Processing;
            await jobRepository.UpdateAsync(job, context.CancellationToken);

            var viaCepResponse = await viaCepClient.GetAddressAsync(message.Cep, context.CancellationToken) 
                ?? throw new Exception("Failed to get address from ViaCEP");

            logger.LogInformation(
                "Job {JobId} completed successfully. Address: {Address}, {City} - {State}",
                message.JobId,
                viaCepResponse.Logradouro,
                viaCepResponse.Localidade,
                viaCepResponse.Uf);

            job.Status = JobStatus.Finished;
            job.CompletedAt = DateTime.UtcNow;
            
            await jobRepository.UpdateAsync(job, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing job {JobId}", message.JobId);

            var job = await jobRepository.GetByIdAsync(message.JobId, context.CancellationToken);
            if (job != null)
            {
                job.Status = JobStatus.Error;
                job.CompletedAt = DateTime.UtcNow;

                await jobRepository.UpdateAsync(job, context.CancellationToken);
            }

            throw;
        }
    }
}
