using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

namespace SourceBase.Web.Auth;

public class BlazorAuthStateProvider(ProtectedLocalStorage localStorage, IHttpClientFactory httpClientFactory) : AuthenticationStateProvider
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private bool _initialized;
    private ClaimsPrincipal _currentPrincipal = Anonymous;

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public UserInfoResponse? UserInfo { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(_currentPrincipal));
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;

        try
        {
            var accessToken = await localStorage.GetAsync<string>(AccessTokenKey);
            var refreshToken = await localStorage.GetAsync<string>(RefreshTokenKey);

            AccessToken = accessToken is { Success: true, Value.Length: > 0 } ? accessToken.Value : null;
            RefreshToken = refreshToken is { Success: true, Value.Length: > 0 } ? refreshToken.Value : null;
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSException)
        {
        }

        if (string.IsNullOrWhiteSpace(AccessToken))
            return;

        try
        {
            await EnsureHydratedAsync();
        }
        catch (HttpRequestException)
        {
            await ClearAsync();
        }
        catch (InvalidOperationException)
        {
            await ClearAsync();
        }
        catch (JsonException)
        {
            await ClearAsync();
        }
        catch (NotSupportedException)
        {
            await ClearAsync();
        }
    }

    public async Task SignInAsync(LoginTokensResponse tokens)
    {
        await SetTokensAsync(tokens.AccessToken, tokens.RefreshToken);

        if (!await EnsureHydratedAsync())
            throw new InvalidOperationException("Authenticated user info could not be loaded.");
    }

    public async Task SignOutAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(AccessToken))
            {
                using var client = CreateAuthHttpClient();
                var response = await client.PostAsync("/api/auth/logout", null);

                if (response.StatusCode != HttpStatusCode.Unauthorized)
                    response.EnsureSuccessStatusCode();
            }
        }
        finally
        {
            await ClearAsync();
        }
    }

    public async Task ClearAsync()
    {
        AccessToken = null;
        RefreshToken = null;
        UserInfo = null;
        _currentPrincipal = Anonymous;

        try
        {
            await localStorage.DeleteAsync(AccessTokenKey);
            await localStorage.DeleteAsync(RefreshTokenKey);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSException)
        {
        }

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        UserInfo = null;
        _currentPrincipal = Anonymous;

        try
        {
            await localStorage.SetAsync(AccessTokenKey, accessToken);
            await localStorage.SetAsync(RefreshTokenKey, refreshToken);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSException)
        {
        }
    }

    private async Task<bool> EnsureHydratedAsync()
    {
        var userInfo = await TryGetUserInfoAsync();
        if (userInfo is not null)
        {
            SetUserInfo(userInfo);
            return true;
        }

        if (string.IsNullOrWhiteSpace(RefreshToken))
        {
            await ClearAsync();
            return false;
        }

        using var client = CreateAuthHttpClient();
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            token = RefreshToken,
        }, JsonOptions);

        if (refreshResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            await ClearAsync();
            return false;
        }

        refreshResponse.EnsureSuccessStatusCode();

        var tokens = await refreshResponse.Content.ReadFromJsonAsync<LoginTokensResponse>(JsonOptions)
            ?? throw new InvalidOperationException("Refresh response did not include tokens.");

        await SetTokensAsync(tokens.AccessToken, tokens.RefreshToken);

        userInfo = await TryGetUserInfoAsync();
        if (userInfo is null)
        {
            await ClearAsync();
            return false;
        }

        SetUserInfo(userInfo);
        return true;
    }

    private async Task<UserInfoResponse?> TryGetUserInfoAsync()
    {
        try
        {
            using var client = CreateAuthHttpClient();
            return await client.GetFromJsonAsync<UserInfoResponse>("/api/auth/info", JsonOptions);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }
    }

    private HttpClient CreateAuthHttpClient()
    {
        var client = httpClientFactory.CreateClient("auth-api");

        if (!string.IsNullOrWhiteSpace(AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        return client;
    }

    private void SetUserInfo(UserInfoResponse userInfo)
    {
        UserInfo = userInfo;
        _currentPrincipal = CreatePrincipal(userInfo);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static ClaimsPrincipal CreatePrincipal(UserInfoResponse userInfo)
    {
        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString()),
            new Claim(ClaimTypes.Name, userInfo.UserName ?? string.Empty),
            ..userInfo.Roles.Select(role => new Claim(ClaimTypes.Role, role))
        ];

        if (!string.IsNullOrWhiteSpace(userInfo.Email))
            claims.Add(new Claim(ClaimTypes.Email, userInfo.Email));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "api-auth"));
    }
}
public sealed record UserInfoResponse(Guid Id, string? UserName, string? Email, string? FirstName, string? LastName, string? PhoneNumber, string[] Roles);

public sealed record LoginTokensResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType);
