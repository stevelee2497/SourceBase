namespace SourceBase.Domain.Entities;

public class EmailEntity(string to, string subject, string body)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string To { get; set; } = to;
    public string Subject { get; set; } = subject;
    public string Body { get; set; } = body;
    public DateTime SentOn { get; set; } = DateTime.UtcNow;
}
