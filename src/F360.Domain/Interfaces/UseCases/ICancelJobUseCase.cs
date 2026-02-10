namespace F360.Domain.Interfaces.UseCases;

public interface ICancelJobUseCase
{
    Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken);
}
