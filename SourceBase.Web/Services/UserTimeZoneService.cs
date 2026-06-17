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
}
