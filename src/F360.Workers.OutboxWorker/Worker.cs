using F360.Domain.Dtos.Messages;
using F360.Domain.Enums;
using F360.Domain.Interfaces;
using F360.Domain.Interfaces.Repositories;
using System.Text.Json;

namespace F360.Workers.OutboxWorker;

public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IOutboxRepository outboxRepository, IMessagePublisher messagePublisher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("OutboxWorker started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        var tasks = Enumerable.Range(0, 10).Select(_ => ProcessSingleMessageAsync(cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessSingleMessageAsync(CancellationToken cancellationToken)
    {
        //using var scope = serviceProvider.CreateScope();
        //var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        //var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var outboxMessage = await outboxRepository.GetAndLockNextPendingMessageAsync(cancellationToken);

        if (outboxMessage == null)
        {
            return;
        }

        try
        {
            var message = JsonSerializer.Deserialize<JobMessage>(outboxMessage.Payload);
            if (message == null)
            {
                logger.LogWarning("Failed to deserialize message {MessageId}", outboxMessage.Id);
                return;
            }

            await messagePublisher.PublishAsync(message);

            outboxMessage.Status = OutboxStatus.Sent;
            outboxMessage.SentAt = DateTime.UtcNow;
            outboxMessage.LockedUntil = null;

            await outboxRepository.UpdateAsync(outboxMessage, cancellationToken);

            logger.LogInformation("Message {MessageId} published successfully", outboxMessage.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing message {MessageId}", outboxMessage.Id);

            outboxMessage.RetryCount++;
            outboxMessage.LockedUntil = null;

            if (outboxMessage.RetryCount >= 3)
            {
                outboxMessage.Status = OutboxStatus.Error;
                outboxMessage.ErrorMessage = ex.Message;
                logger.LogError("Message {MessageId} moved to Error status after 3 retries", outboxMessage.Id);
            }

            await outboxRepository.UpdateAsync(outboxMessage, cancellationToken);
        }
    }
}
