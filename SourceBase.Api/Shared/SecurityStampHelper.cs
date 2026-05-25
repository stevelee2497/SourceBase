namespace SourceBase.Api.Shared;

public static class SecurityStampHelper
{
    public static string Generate()
    {
        return Guid.NewGuid().ToString();
    }
}
