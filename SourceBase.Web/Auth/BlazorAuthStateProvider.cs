using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using SourceBase.Web.Services;

namespace SourceBase.Web.Auth;

public class BlazorAuthStateProvider(ProtectedLocalStorage localStorage) : AuthenticationStateProvider
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private bool _initialized;
    private ClaimsPrincipal _currentPrincipal = Anonymous;

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public UserInfoResponse? UserInfo { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(_currentPrincipal));

    // Loads tokens from localStorage. Returns true if an access token was found.
    public async Task<bool> LoadTokensAsync()
    {
        if (_initialized)
            return !string.IsNullOrWhiteSpace(AccessToken);

        _initialized = true;

        var accessToken = await localStorage.GetAsync<string>(AccessTokenKey);
        var refreshToken = await localStorage.GetAsync<string>(RefreshTokenKey);

        AccessToken = accessToken is { Success: true, Value.Length: > 0 } ? accessToken.Value : null;
        RefreshToken = refreshToken is { Success: true, Value.Length: > 0 } ? refreshToken.Value : null;

        return !string.IsNullOrWhiteSpace(AccessToken);
    }

    // Saves tokens to localStorage and exposes them for AuthHeaderHandler.
    public async Task SetTokensAsync(LoginResponse tokens)
    {
        AccessToken = tokens.AccessToken;
        RefreshToken = tokens.RefreshToken;
        UserInfo = null;
        _currentPrincipal = Anonymous;

        await localStorage.SetAsync(AccessTokenKey, tokens.AccessToken);
        await localStorage.SetAsync(RefreshTokenKey, tokens.RefreshToken);
    }

    // Sets the authenticated principal and notifies listeners.
    public void SetUserInfo(UserInfoResponse userInfo)
    {
        UserInfo = userInfo;
        _currentPrincipal = CreatePrincipal(userInfo);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    // Clears all auth state, removes tokens from localStorage, and notifies listeners.
    public async Task SignOutAsync()
    {
        AccessToken = null;
        RefreshToken = null;
        UserInfo = null;
        _currentPrincipal = Anonymous;

        await localStorage.DeleteAsync(AccessTokenKey);
        await localStorage.DeleteAsync(RefreshTokenKey);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static ClaimsPrincipal CreatePrincipal(UserInfoResponse userInfo)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString()),
            new Claim(ClaimTypes.Name, userInfo.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, userInfo.Email ?? string.Empty),
            ..userInfo.Roles.Select(role => new Claim(ClaimTypes.Role, role))
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "api-auth"));
    }
}
