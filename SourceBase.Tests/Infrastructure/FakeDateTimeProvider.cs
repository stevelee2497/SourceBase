using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Tests.Infrastructure;

public class FakeDateTimeProvider : IDateTime
{
    private TimeSpan _offset = TimeSpan.Zero;
    public DateTime UtcNow => DateTimeOffset.UtcNow.Add(_offset).UtcDateTime;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow.Add(_offset);
    public void Advance(TimeSpan duration) => _offset += duration;
}
