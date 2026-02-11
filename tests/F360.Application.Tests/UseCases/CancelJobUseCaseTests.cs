using Bogus;
using F360.Application.UseCases;
using F360.Domain.Entities;
using F360.Domain.Enums;
using F360.Domain.Exceptions;
using F360.Domain.Interfaces.Database.Repositories;
using FluentAssertions;
using NSubstitute;

namespace F360.Application.Tests.UseCases;

public class CancelJobUseCaseTests
{
    private readonly IJobRepository _jobRepository;
    private readonly CancelJobUseCase _cancelJobUseCase;

    public CancelJobUseCaseTests()
    {
        _jobRepository = Substitute.For<IJobRepository>();
        _cancelJobUseCase = new CancelJobUseCase(_jobRepository);
    }

    [Fact]
    public async Task ExecuteAsync_JobNotFound_ThrowsNotFoundException()
    {
        var jobId = Guid.NewGuid();
        _jobRepository.GetByIdAsync(Arg.Any<Guid>(), CancellationToken.None)
            .Returns((Job?)null);

        var act = async () => await _cancelJobUseCase.ExecuteAsync(jobId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Job not found");
    }

    [Theory]
    [InlineData(JobStatus.Processing)]
    [InlineData(JobStatus.Finished)]
    [InlineData(JobStatus.Error)]
    [InlineData(JobStatus.Cancelled)]
    public async Task ExecuteAsync_NonPendingJob_ThrowsBusinessException(JobStatus status)
    {
        var job = new Faker<Job>()
             .RuleFor(j => j.Id, f => Guid.NewGuid())
             .RuleFor(j => j.Cep, f => f.Address.ZipCode("#####-###"))
             .RuleFor(j => j.Priority, f => f.PickRandom<JobPriority>())
             .RuleFor(j => j.Status, status)
             .RuleFor(j => j.CreatedAt, f => f.Date.Past())
             .Generate();

        _jobRepository.GetByIdAsync(Arg.Any<Guid>(), CancellationToken.None).Returns(job);

        var act = async () => await _cancelJobUseCase.ExecuteAsync(job.Id, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Only pending jobs can be cancelled");
    }

    [Fact]
    public async Task ExecuteAsync_PendingJob_CancelsSuccessfully()
    {
        var job = new Faker<Job>()
            .RuleFor(j => j.Id, f => Guid.NewGuid())
            .RuleFor(j => j.Cep, f => f.Address.ZipCode("#####-###"))
            .RuleFor(j => j.Priority, f => f.PickRandom<JobPriority>())
            .RuleFor(j => j.Status, JobStatus.Pending)
            .RuleFor(j => j.CreatedAt, f => f.Date.Past())
            .Generate();

        var jobId = Guid.NewGuid();

        _jobRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(job);

        await _cancelJobUseCase.ExecuteAsync(job.Id, CancellationToken.None);

        job.Status.Should().Be(JobStatus.Cancelled);
        job.CompletedAt.Should().NotBeNull();
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        await _jobRepository.Received(1).UpdateAsync(job, CancellationToken.None);
    }
}