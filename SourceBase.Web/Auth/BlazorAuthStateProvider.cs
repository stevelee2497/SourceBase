using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using SourceBase.Web.Services;

namespace SourceBase.Web.Auth;

public class BlazorAuthStateProvider(IJSRuntime js) : AuthenticationStateProvider
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

        try
        {
            AccessToken = await js.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
            RefreshToken = await js.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);
        }
        catch
        {
            AccessToken = null;
            RefreshToken = null;
        }

        return !string.IsNullOrWhiteSpace(AccessToken);
    }

    // Saves tokens to localStorage without dropping the current user snapshot.
    public async Task SetTokensAsync(LoginResponse tokens)
    {
        _initialized = true;
        AccessToken = tokens.AccessToken;
        RefreshToken = tokens.RefreshToken;

        await js.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, tokens.AccessToken);
        await js.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, tokens.RefreshToken);
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
        _initialized = true;
        AccessToken = null;
        RefreshToken = null;
        UserInfo = null;
        _currentPrincipal = Anonymous;

        await js.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
        await js.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);

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
