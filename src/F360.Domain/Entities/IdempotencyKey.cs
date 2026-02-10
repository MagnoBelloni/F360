namespace F360.Domain.Entities;

public class IdempotencyKey
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public Guid JobId { get; set; }

    public DateTime CreatedAt { get; set; }
}
