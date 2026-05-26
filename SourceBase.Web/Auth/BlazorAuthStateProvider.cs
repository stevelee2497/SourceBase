using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace SourceBase.Web.Auth;

// Stores access/refresh tokens in ProtectedLocalStorage (encrypted browser storage)
// and exposes them in-memory for the current circuit — same pattern as a React app
// using localStorage.setItem('token', ...).
public class BlazorAuthStateProvider(ProtectedLocalStorage localStorage) : AuthenticationStateProvider
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private bool _initialized;

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal()));

        var claims = ParseJwtClaims(AccessToken);
        var identity = new ClaimsIdentity(claims, "jwt");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    // Called once after the Blazor circuit connects so ProtectedLocalStorage is available
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            var at = await localStorage.GetAsync<string>(AccessTokenKey);
            var rt = await localStorage.GetAsync<string>(RefreshTokenKey);
            if (at.Success && !string.IsNullOrEmpty(at.Value))
            {
                AccessToken = at.Value;
                RefreshToken = rt.Success ? rt.Value : null;
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
            }
        }
        catch { /* storage not yet available or corrupted */ }
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        try
        {
            await localStorage.SetAsync(AccessTokenKey, accessToken);
            await localStorage.SetAsync(RefreshTokenKey, refreshToken);
        }
        catch { /* ignore */ }
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task ClearAsync()
    {
        AccessToken = null;
        RefreshToken = null;
        try
        {
            await localStorage.DeleteAsync(AccessTokenKey);
            await localStorage.DeleteAsync(RefreshTokenKey);
        }
        catch { /* ignore */ }
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    // JWT payload is base64url-decoded to extract claims (sub, email, name, role)
    // — no external JWT library needed.
    private static IEnumerable<Claim> ParseJwtClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) return [];

        var payload = parts[1];
        var padded = (payload.Length % 4) switch
        {
            2 => payload + "==",
            3 => payload + "=",
            _ => payload
        };
        padded = padded.Replace('-', '+').Replace('_', '/');

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject()
                .SelectMany(p => MapToClaims(p.Name, p.Value))
                .ToList();
        }
        catch { return []; }
    }

    private static IEnumerable<Claim> MapToClaims(string key, JsonElement value)
    {
        var type = key switch
        {
            "sub" => ClaimTypes.NameIdentifier,
            "email" => ClaimTypes.Email,
            "name" => ClaimTypes.Name,
            "role" => ClaimTypes.Role,
            _ => key
        };
        if (value.ValueKind == JsonValueKind.Array)
            return value.EnumerateArray().Select(v => new Claim(type, v.GetString() ?? string.Empty));
        return [new Claim(type, value.GetString() ?? value.GetRawText())];
    }
}
