using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SourceBase.Desktop.Models;

namespace SourceBase.Desktop.Services;

/// <summary>
/// Fire-and-forget client that POSTs habit log actions to the SourceBase API.
/// Silently no-ops when ApiBaseUrl/ApiToken are not configured or the API is unreachable.
/// </summary>
public sealed class HabitLogService(Func<AppSettings> settings)
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void LogHabitStarted(string? habitId, string? habitName) => Send(habitId, habitName, "HabitStarted");
    public void LogDismissed() => Send(null, null, "Dismissed");
    public void LogSnoozed() => Send(null, null, "Snoozed");
    public void LogSuppressedVideo() => Send(null, null, "SuppressedVideo");

    private void Send(string? habitId, string? habitName, string action) => _ = SendAsync(habitId, habitName, action);

    private async Task SendAsync(string? habitId, string? habitName, string action)
    {
        var s = settings();
        if (string.IsNullOrWhiteSpace(s.ApiBaseUrl) || string.IsNullOrWhiteSpace(s.ApiToken)) return;

        try
        {
            var body = JsonSerializer.Serialize(new { habitId, habitName, action, occurredAt = DateTime.UtcNow }, JsonOpts);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{s.ApiBaseUrl.TrimEnd('/')}/api/habit-logs")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", s.ApiToken) },
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            await Http.SendAsync(req);
        }
        catch { }
    }
}
