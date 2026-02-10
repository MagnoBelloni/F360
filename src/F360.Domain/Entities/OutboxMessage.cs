using F360.Domain.Enums;

namespace F360.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public string Payload { get; set; } = string.Empty;

    public JobPriority Priority { get; set; }

    public OutboxStatus Status { get; set; }

    public DateTime ScheduledTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? LockedUntil { get; set; }

    public int RetryCount { get; set; }

    public string? ErrorMessage { get; set; }
}
