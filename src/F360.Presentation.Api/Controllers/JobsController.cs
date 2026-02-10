using F360.Application.DTOs.Requests;
using F360.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace F360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController(
    CreateJobUseCase createJobUseCase,
    CancelJobUseCase cancelJobUseCase,
    GetJobUseCase getJobUseCase) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            return BadRequest(new { error = "Idempotency-Key header is required" });
        }

        var result = await createJobUseCase.ExecuteAsync(request, idempotencyKey, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await getJobUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}:cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await cancelJobUseCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
