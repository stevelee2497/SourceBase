using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Tests.Infrastructure;

public class FakeDateTimeProvider : IDateTime
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
    public DateTime UtcNow => _utcNow.UtcDateTime;
    public DateTimeOffset UtcNowOffset => _utcNow;
    public void Advance(TimeSpan duration) => _utcNow += duration;
}
