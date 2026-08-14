namespace SourceBase.Web.Services;

public class UserTimeZoneService
{
    private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

    public bool IsInitialized { get; private set; }
    public event Action? OnChange;

    public void SetTimeZone(string ianaTimeZoneId)
    {
        try { _timeZone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId); }
        catch { _timeZone = TimeZoneInfo.Utc; }
        IsInitialized = true;
        OnChange?.Invoke();
    }

    public DateTime ToLocalTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc.ToUniversalTime(), _timeZone);

    // Calendar date in the user's zone — not the browser runtime's ambient local date.
    public DateTime Today => ToLocalTime(DateTime.UtcNow).Date;

    public DateTime ToUtc(DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return DateTime.SpecifyKind(unspecified - _timeZone.GetUtcOffset(unspecified), DateTimeKind.Utc);
    }
}
