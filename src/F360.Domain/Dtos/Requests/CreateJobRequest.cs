using F360.Domain.Enums;

namespace F360.Application.DTOs.Requests;

public class CreateJobRequest
{
    public string Cep { get; set; } = string.Empty;
    public JobPriority Priority { get; set; }
    public DateTime? ScheduledTime { get; set; }
}
