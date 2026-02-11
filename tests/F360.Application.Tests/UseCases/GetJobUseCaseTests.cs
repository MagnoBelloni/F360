using Bogus;
using F360.Application.UseCases;
using F360.Domain.Entities;
using F360.Domain.Enums;
using F360.Domain.Exceptions;
using F360.Domain.Interfaces.Database.Repositories;
using FluentAssertions;
using NSubstitute;

namespace F360.Application.Tests.UseCases;

public class GetJobUseCaseTests
{
    private readonly IJobRepository _jobRepository;
    private readonly GetJobUseCase getJobUseCase;

    public GetJobUseCaseTests()
    {
        _jobRepository = Substitute.For<IJobRepository>();
        getJobUseCase = new GetJobUseCase(_jobRepository);
    }

    [Fact]
    public async Task ExecuteAsync_JobNotFound_ThrowsNotFoundException()
    {
        var jobId = Guid.NewGuid();

        _jobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Job?)null);

        var act = async () => await getJobUseCase.ExecuteAsync(jobId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Job not found");
    }

    [Fact]
    public async Task ExecuteAsync_JobExists_ReturnsJobResponse()
    {
        var job = new Faker<Job>()
            .RuleFor(j => j.Id, f => Guid.NewGuid())
            .RuleFor(j => j.Cep, f => f.Address.ZipCode("#####-###"))
            .RuleFor(j => j.Priority, f => f.PickRandom<JobPriority>())
            .RuleFor(j => j.Status, f => f.PickRandom<JobStatus>())
            .RuleFor(j => j.CreatedAt, f => f.Date.Past())
            .RuleFor(j => j.CompletedAt, f => f.Date.Past())
            .Generate();

        _jobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(job);

        var result = await getJobUseCase.ExecuteAsync(job.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(job.Id);
        result.Cep.Should().Be(job.Cep);
        result.Priority.Should().Be(job.Priority);
        result.Status.Should().Be(job.Status);
        result.ScheduledTime.Should().Be(job.ScheduledTime);
        result.CreatedAt.Should().Be(job.CreatedAt);
        result.CompletedAt.Should().Be(job.CompletedAt);
    }
}