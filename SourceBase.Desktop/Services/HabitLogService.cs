using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SourceBase.Desktop.Models;

namespace SourceBase.Desktop.Services;

/// <summary>
/// Fire-and-forget client that batch-POSTs habit log actions to the SourceBase API.
/// Silently no-ops when ApiBaseUrl/ApiToken are not configured or the API is unreachable.
/// Automatically refreshes the access token via ApiRefreshToken on 401 and retries once.
/// </summary>
public sealed class HabitLogService(Func<AppSettings> settings, Action onSave)
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
            var url = $"{s.ApiBaseUrl.TrimEnd('/')}/api/habit-logs";

            var resp = await PostAsync(url, body, s.ApiToken);

            if (resp.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshAsync(s))
                await PostAsync(url, body, s.ApiToken!);
        }
        catch { }
    }

    private static async Task<HttpResponseMessage> PostAsync(string url, string body, string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        return await Http.SendAsync(req);
    }

    private async Task<bool> TryRefreshAsync(AppSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.ApiRefreshToken)) return false;
        try
        {
            var payload = JsonSerializer.Serialize(new { token = s.ApiRefreshToken }, JsonOpts);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{s.ApiBaseUrl!.TrimEnd('/')}/api/auth/refresh")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return false;

            var tokens = await JsonSerializer.DeserializeAsync<TokenResponse>(
                await resp.Content.ReadAsStreamAsync(), JsonOpts);
            if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken)) return false;

            s.ApiToken = tokens.AccessToken;
            s.ApiRefreshToken = tokens.RefreshToken;
            onSave();
            return true;
        }
        catch { return false; }
    }

    private record Entry(string? HabitId, string? HabitName, string Action, DateTime OccurredAt);
    private record TokenResponse(string AccessToken, string RefreshToken);
}
