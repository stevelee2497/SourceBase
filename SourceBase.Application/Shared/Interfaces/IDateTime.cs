namespace SourceBase.Application.Shared.Interfaces;

public interface IDateTime
{
    DateTime UtcNow { get; }
    DateTimeOffset UtcNowOffset { get; }
}
