using F360.Application.DTOs.Requests;
using F360.Domain.Dtos.Responses;

namespace F360.Domain.Interfaces.UseCases;

public interface ICreateJobUseCase
{
    Task<CreateJobResponse> ExecuteAsync(CreateJobRequest request, string idempotencyKey, CancellationToken cancellationToken);
}
