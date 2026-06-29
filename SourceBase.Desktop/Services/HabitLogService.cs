using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SourceBase.Desktop.Models;

namespace SourceBase.Desktop.Services;

/// <summary>
/// Fire-and-forget client that batch-POSTs habit log actions to the SourceBase API.
/// Silently no-ops when ApiBaseUrl/ApiToken are not configured or the API is unreachable.
/// </summary>
public sealed class HabitLogService(Func<AppSettings> settings)
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void LogHabitsStarted(IEnumerable<Habit> habits)
    {
        var now = DateTime.UtcNow;
        Send(habits.Select(h => new Entry(h.Id, h.Name, "HabitStarted", now)));
    }

    public void LogDismissed() => Send([new Entry(null, null, "Dismissed", DateTime.UtcNow)]);
    public void LogSnoozed() => Send([new Entry(null, null, "Snoozed", DateTime.UtcNow)]);
    public void LogSuppressedVideo() => Send([new Entry(null, null, "SuppressedVideo", DateTime.UtcNow)]);

    private void Send(IEnumerable<Entry> entries) => _ = SendAsync(entries);

    private async Task SendAsync(IEnumerable<Entry> entries)
    {
        var s = settings();
        if (string.IsNullOrWhiteSpace(s.ApiBaseUrl) || string.IsNullOrWhiteSpace(s.ApiToken)) return;

        try
        {
            var body = JsonSerializer.Serialize(new { entries }, JsonOpts);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{s.ApiBaseUrl.TrimEnd('/')}/api/habit-logs")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", s.ApiToken) },
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            await Http.SendAsync(req);
        }
        catch { }
    }

    private record Entry(string? HabitId, string? HabitName, string Action, DateTime OccurredAt);
}
