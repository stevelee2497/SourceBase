using Xunit;

namespace SourceBase.Tests.Infrastructure;

/// <summary>
/// Marks a test that requires the Redis test container.
/// The test is automatically skipped when USE_REDIS is not set to "true".
/// </summary>
public sealed class RequiresRedisFactAttribute : FactAttribute
{
    private static readonly bool UseRedis = string.Equals(
        Environment.GetEnvironmentVariable("USE_REDIS"), "true", StringComparison.OrdinalIgnoreCase);

    public RequiresRedisFactAttribute()
    {
        if (!UseRedis)
            Skip = "Requires Redis test container (USE_REDIS=true)";
    }
}
