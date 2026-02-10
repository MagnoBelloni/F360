using F360.Domain.Enums;

namespace F360.Domain.Dtos.Messages;

public class JobMessage
{
    public Guid JobId { get; set; }
    public string Cep { get; set; } = string.Empty;
    public JobPriority Priority { get; set; }
}
