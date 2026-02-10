using F360.Domain.Enums;

namespace F360.Domain.Entities;

public class Job
{
    public Guid Id { get; set; }

    public string Cep { get; set; } = string.Empty;

    public JobPriority Priority { get; set; }

    public JobStatus Status { get; set; }

    public DateTime? ScheduledTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
