namespace SourceBase.Domain.Entities;

public class EmailEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public DateTime SentOn { get; set; }
}
