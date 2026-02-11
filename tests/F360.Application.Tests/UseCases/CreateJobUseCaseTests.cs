using Bogus;
using F360.Application.DTOs.Requests;
using F360.Application.UseCases;
using F360.Application.Validators;
using F360.Domain.Entities;
using F360.Domain.Enums;
using F360.Domain.Exceptions;
using F360.Domain.Interfaces.Database.Repositories;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace F360.Application.Tests.UseCases;

public class CreateJobUseCaseTests
{
    private readonly IJobRepository _jobRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly CreateJobRequestValidator _validator;
    private readonly CreateJobUseCase _createJobUseCase;
    private readonly Faker _faker;

    public CreateJobUseCaseTests()
    {
        _jobRepository = Substitute.For<IJobRepository>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _idempotencyRepository = Substitute.For<IIdempotencyRepository>();
        _validator = new CreateJobRequestValidator();

        _createJobUseCase = new CreateJobUseCase(
            _jobRepository,
            _outboxRepository,
            _idempotencyRepository,
            _validator);

        _faker = new Faker();
    }

    [Fact]
    public async Task ExecuteAsync_InvalidCep_ThrowsBusinessException()
    {
        var request = new Faker<CreateJobRequest>()
            .RuleFor(r => r.Cep, "invalid-cep")
            .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>());

        var idempotencyKey = _faker.Random.AlphaNumeric(32);

        var act = async () => await _createJobUseCase.ExecuteAsync(request, idempotencyKey, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Invalid CEP format");
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateIdempotencyKey_ThrowsConflictException()
    {
        var request = new Faker<CreateJobRequest>()
           .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
           .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>());

        var idempotencyKey = _faker.Random.AlphaNumeric(32);

        _idempotencyRepository.GetByKeyAsync(idempotencyKey, CancellationToken.None)
            .Returns(new IdempotencyKey { Key = idempotencyKey });

        var act = async () => await _createJobUseCase.ExecuteAsync(request, idempotencyKey, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Duplicate request detected");
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateIdempotencyKey_WhileCreateAsync_ThrowsConflictException()
    {
        var request = new Faker<CreateJobRequest>()
            .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
            .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>());

        var idempotencyKey = _faker.Random.AlphaNumeric(32);

        _idempotencyRepository
            .CreateAsync(Arg.Any<IdempotencyKey>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("DuplicateKey"));

        var act = async () => await _createJobUseCase.ExecuteAsync(request, idempotencyKey, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Duplicate request detected");
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_ReturnsJobId()
    {
        var request = new Faker<CreateJobRequest>()
            .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
            .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>())
            .RuleFor(r => r.ScheduledTime, f => f.Date.Future())
            .Generate();

        var idempotencyKey = _faker.Random.AlphaNumeric(32);

        _idempotencyRepository.GetByKeyAsync(idempotencyKey, CancellationToken.None)
            .Returns((IdempotencyKey?)null);

        var result = await _createJobUseCase.ExecuteAsync(request, idempotencyKey, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();

        await _idempotencyRepository.Received(1).CreateAsync(
            Arg.Is<IdempotencyKey>(r => r.Key == idempotencyKey),
            Arg.Any<CancellationToken>());

        await _jobRepository.Received(1).CreateAsync(
            Arg.Is<Job>(j => j.Cep == request.Cep && j.Priority == request.Priority),
            Arg.Any<CancellationToken>());

        await _outboxRepository.Received(1).CreateAsync(
            Arg.Is<OutboxMessage>(m => m.JobId == result.Id), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task ExecuteAsync_ScheduledTimeNull_UsesUtcNow()
    {
        var request = new Faker<CreateJobRequest>()
            .RuleFor(r => r.Cep, f => f.Address.ZipCode("#####-###"))
            .RuleFor(r => r.Priority, f => f.PickRandom<JobPriority>())
            .RuleFor(r => r.ScheduledTime, (DateTime?)null);

        var idempotencyKey = _faker.Random.AlphaNumeric(32);

        await _createJobUseCase.ExecuteAsync(request, idempotencyKey, CancellationToken.None);

        await _outboxRepository.Received(1).CreateAsync(
            Arg.Is<OutboxMessage>(m => m.ScheduledTime <= DateTime.UtcNow), Arg.Any<CancellationToken>());
    }
}