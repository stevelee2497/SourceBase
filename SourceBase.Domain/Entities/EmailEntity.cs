using System.Diagnostics.CodeAnalysis;

namespace SourceBase.Domain.Entities;

public class EmailEntity
{
    [SetsRequiredMembers]
    public EmailEntity(string to, string subject, string body)
    {
        To = to;
        Subject = subject;
        Body = body;
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public DateTime SentOn { get; set; } = DateTime.UtcNow;
}
