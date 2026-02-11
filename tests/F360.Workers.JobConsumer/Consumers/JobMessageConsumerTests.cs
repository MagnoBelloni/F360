using Bogus;
using F360.Domain.Dtos.Messages;
using F360.Domain.Dtos.Responses.External;
using F360.Domain.Entities;
using F360.Domain.Enums;
using F360.Domain.Interfaces.Database.Repositories;
using F360.Domain.Interfaces.HttpClients;
using F360.Workers.JobConsumer.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace F360.Workers.JobConsumer.Tests.Consumers;

public class JobMessageConsumerTests
{
    private readonly ILogger<JobMessageConsumer> _logger;
    private readonly IJobRepository _jobRepository;
    private readonly IViaCepClient _viaCepClient;
    private readonly JobMessageConsumer _jobMessageConsumer;
    private readonly ConsumeContext<JobMessage> _context;

    public JobMessageConsumerTests()
    {
        _logger = Substitute.For<ILogger<JobMessageConsumer>>();
        _jobRepository = Substitute.For<IJobRepository>();
        _viaCepClient = Substitute.For<IViaCepClient>();
        _context = Substitute.For<ConsumeContext<JobMessage>>();
        _jobMessageConsumer = new JobMessageConsumer(_logger, _jobRepository, _viaCepClient);
    }

    [Fact]
    public async Task Consume_JobNotFound_LogsWarningAndReturns()
    {
        var message = new Faker<JobMessage>()
           .RuleFor(r => r.JobId, f => Guid.NewGuid())
           .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
           .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>());

        _context.Message.Returns(message);

        _jobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Job?)null);

        await _jobMessageConsumer.Consume(_context);

        await _viaCepClient.DidNotReceive().GetAddressAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _jobRepository.DidNotReceive().UpdateAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_CancelledJob_SkipsProcessing()
    {
        var message = new Faker<JobMessage>()
          .RuleFor(r => r.JobId, f => Guid.NewGuid())
          .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
          .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>());

        var job = new Faker<Job>()
            .RuleFor(r => r.Id, f => Guid.NewGuid())
            .RuleFor(r => r.Status, JobStatus.Cancelled);

        _context.Message.Returns(message);
        _jobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(job);

        await _jobMessageConsumer.Consume(_context);

        await _viaCepClient.DidNotReceive().GetAddressAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_ViaCepFails_MarksJobAsError()
    {
        var message = new Faker<JobMessage>()
          .RuleFor(r => r.JobId, f => Guid.NewGuid())
          .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
          .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>());

        var job = new Faker<Job>()
            .RuleFor(r => r.Id, f => Guid.NewGuid())
            .RuleFor(r => r.Status, JobStatus.Pending)
            .Generate();

        _context.Message.Returns(message);

        _jobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(job);
        
        _viaCepClient.GetAddressAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API Error"));

        var act = async () => await _jobMessageConsumer.Consume(_context);

        await act.Should().ThrowAsync<HttpRequestException>();
        job.Status.Should().Be(JobStatus.Error);
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Consume_ValidMessage_ProcessesSuccessfully()
    {
        var message = new Faker<JobMessage>()
          .RuleFor(r => r.JobId, f => Guid.NewGuid())
          .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
          .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>())
          .Generate();

        var job = new Faker<Job>()
            .RuleFor(r => r.Id, f => Guid.NewGuid())
            .RuleFor(r => r.Status, JobStatus.Pending)
            .Generate();

        var viaCepResponse = new Faker<ViaCepResponse>()
            .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
            .RuleFor(r => r.Logradouro, f => f.Address.StreetName())
            .RuleFor(r => r.Localidade, f => f.Address.City())
            .RuleFor(r => r.Uf, f => f.Address.StateAbbr());
        
        _context.Message.Returns(message);
        
        _jobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
           .Returns(job);

        _viaCepClient.GetAddressAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(viaCepResponse);

        await _jobMessageConsumer.Consume(_context);

        job.Status.Should().Be(JobStatus.Finished);
        job.CompletedAt.Should().NotBeNull();

        await _jobRepository.Received(2).UpdateAsync(job, Arg.Any<CancellationToken>());
        await _viaCepClient.Received(1).GetAddressAsync(message.Cep, Arg.Any<CancellationToken>());
    }
}