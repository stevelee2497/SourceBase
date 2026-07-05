using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SourceBase.Desktop.Models;

namespace SourceBase.Desktop.Services;

/// <summary>
/// Reports machine status (Active/Inactive) to the SourceBase API via POST /api/machines.
/// Fire-and-forget client that silently no-ops when ApiBaseUrl/ApiToken are not configured or the API is unreachable.
/// Automatically refreshes the access token via ApiRefreshToken on 401 and retries once.
/// </summary>
public sealed class MachineStatusService(Func<AppSettings> settings, Action onSave)
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Reports the machine's current status (Active/Inactive) to the API by posting to /api/machines.
    /// If a machine with the same name exists, updates it; otherwise creates a new one.
    /// Returns true if successful, false silently on any error.
    /// Automatically refreshes the access token on 401 and retries once.
    /// </summary>
    public async Task<bool> ReportStatusAsync(string status)
    {
        var s = settings();
        if (string.IsNullOrWhiteSpace(s.ApiBaseUrl) || string.IsNullOrWhiteSpace(s.ApiToken)) return false;

        try
        {
            var body = JsonSerializer.Serialize(
                new { name = Environment.MachineName, status = status.ToLower() == "active" ? "Active" : "Inactive" },
                JsonOpts);
            var url = $"{s.ApiBaseUrl.TrimEnd('/')}/api/machines";

            var resp = await PostAsync(url, body, s.ApiToken);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (!await TryRefreshAsync(s)) { MarkFailed(s); return false; }
                resp = await PostAsync(url, body, s.ApiToken!);
            }

            if (resp.IsSuccessStatusCode) { MarkConnected(s); return true; }
            else { MarkFailed(s); return false; }
        }
        catch { MarkFailed(s); return false; }
    }

    private void MarkConnected(AppSettings s) { s.ApiStatus = ApiConnectionStatus.Connected; onSave(); }
    private void MarkFailed(AppSettings s) { s.ApiStatus = ApiConnectionStatus.Failed; onSave(); }

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

    private record TokenResponse(string AccessToken, string RefreshToken);
}
