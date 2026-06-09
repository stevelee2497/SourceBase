using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Infrastructure.Implementations;

public class DateTimeProvider : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
}
