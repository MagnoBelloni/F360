using Bogus;
using F360.Domain.Dtos.Messages;
using F360.Domain.Entities;
using F360.Domain.Enums;
using F360.Domain.Interfaces;
using F360.Domain.Interfaces.Database.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text.Json;

namespace F360.Workers.OutboxWorker.Tests;

public class WorkerTests
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly Worker _worker;
    private readonly Faker _faker;

    public WorkerTests()
    {
        _logger = Substitute.For<ILogger<Worker>>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _scope = Substitute.For<IServiceScope>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _messagePublisher = Substitute.For<IMessagePublisher>();
        _faker = new Faker();

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(_scope);

        _scope.ServiceProvider.GetService(typeof(IOutboxRepository)).Returns(_outboxRepository);
        _scope.ServiceProvider.GetService(typeof(IMessagePublisher)).Returns(_messagePublisher);

        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        _serviceProvider.CreateScope().Returns(_scope);

        _worker = new Worker(_logger, _serviceProvider);
    }

    [Fact]
    public async Task ProcessOutboxMessages_NoMessages_DoesNotPublish()
    {
        _outboxRepository.GetAndLockNextPendingMessageAsync(Arg.Any<CancellationToken>())
            .Returns((OutboxMessage?)null);

        await _worker.StartAsync(CancellationToken.None);

        await Task.Delay(500);

        await _worker.StopAsync(CancellationToken.None);

        await _messagePublisher.DidNotReceive().PublishAsync(Arg.Any<JobMessage>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_ThirdRetryFails_MovesToError()
    {
        var jobId = Guid.NewGuid();

        var message = new Faker<JobMessage>()
            .RuleFor(r => r.JobId, f => jobId)
            .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
            .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>());

        var outboxMessage = new Faker<OutboxMessage>()
            .RuleFor(r => r.Id, f => Guid.NewGuid())
            .RuleFor(r => r.JobId, f => jobId)
            .RuleFor(r => r.Payload, f => JsonSerializer.Serialize(message))
            .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>())
            .RuleFor(r => r.Status, f => OutboxStatus.Pending)
            .RuleFor(r => r.ScheduledTime, f => f.Date.Past())
            .RuleFor(r => r.CreatedAt, f => f.Date.Past())
            .RuleFor(r => r.RetryCount, f => 2);

        _outboxRepository.GetAndLockNextPendingMessageAsync(Arg.Any<CancellationToken>())
            .Returns(outboxMessage, (OutboxMessage?)null);

        _messagePublisher.PublishAsync(Arg.Any<JobMessage>())
            .ThrowsAsync(new Exception("RabbitMQ connection failed"));

        var executeTask = _worker.StartAsync(CancellationToken.None);
        
        await Task.Delay(500);
        
        await _worker.StopAsync(CancellationToken.None);

        await _outboxRepository.Received().UpdateAsync(Arg.Is<OutboxMessage>(m =>
            m.RetryCount == 3 &&
            m.Status == OutboxStatus.Error &&
            m.ErrorMessage == "RabbitMQ connection failed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessOutboxMessages_ValidMessage_PublishesSuccessfully()
    {
        var jobId = Guid.NewGuid();
        var traceId = _faker.Random.Guid().ToString();

        var message = new Faker<JobMessage>()
            .RuleFor(r => r.JobId, f => jobId)
            .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
            .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>())
            .Generate();

        var outboxMessage = new Faker<OutboxMessage>()
           .RuleFor(r => r.Id, f => Guid.NewGuid())
           .RuleFor(r => r.JobId, f => jobId)
           .RuleFor(r => r.Payload, f => JsonSerializer.Serialize(message))
           .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>())
           .RuleFor(r => r.Status, f => OutboxStatus.Pending)
           .RuleFor(r => r.ScheduledTime, f => f.Date.Past())
           .RuleFor(r => r.CreatedAt, f => f.Date.Past())
           .RuleFor(r => r.RetryCount, f => 0);

        _outboxRepository.GetAndLockNextPendingMessageAsync(Arg.Any<CancellationToken>())
            .Returns(outboxMessage, (OutboxMessage?)null);

        var executeTask = _worker.StartAsync(CancellationToken.None);
        
        await Task.Delay(500);
        
        await _worker.StopAsync(CancellationToken.None);

        await _messagePublisher.Received().PublishAsync(Arg.Is<JobMessage>(m =>
            m.JobId == jobId &&
            m.Cep == message.Cep));

        await _outboxRepository.Received().UpdateAsync(Arg.Is<OutboxMessage>(m =>
            m.Status == OutboxStatus.Sent &&
            m.SentAt != null &&
            m.LockedUntil == null),
            Arg.Any<CancellationToken>());
    }
}