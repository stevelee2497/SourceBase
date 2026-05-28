using System.Net.Http.Headers;
using SourceBase.Web.Auth;

namespace SourceBase.Web.Services;

public class ApiHttpClient(HttpClient http, BlazorAuthStateProvider auth)
{
    private static readonly ErrorResponse UnknownError = new("UNKNOWN", "An unexpected error occurred.");

    private HttpRequestMessage Request(HttpMethod method, string url, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        if (body is not null)
            req.Content = JsonContent.Create(body);
        return req;
    }

    private HttpRequestMessage AuthorizedRequest(HttpMethod method, string url, object? body = null)
    {
        var req = Request(method, url, body);
        if (!string.IsNullOrWhiteSpace(auth.AccessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return req;
    }

    private async Task<(T? data, ErrorResponse? error)> ExecuteAsync<T>(Func<HttpRequestMessage> factory, bool retry = true)
    {
        try
        {
            var response = await http.SendAsync(factory());
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && retry)
            {
                if (!await TryRefreshTokensAsync())
                    return (default, await response.Content.ReadFromJsonAsync<ErrorResponse>() ?? UnknownError);
                response = await http.SendAsync(factory());
            }
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadFromJsonAsync<T>(), null);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (default, error ?? UnknownError);
        }
        catch
        {
            return (default, UnknownError);
        }
    }

    private async Task<ErrorResponse?> ExecuteAsync(Func<HttpRequestMessage> factory, bool retry = true)
    {
        try
        {
            var response = await http.SendAsync(factory());
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && retry)
            {
                if (!await TryRefreshTokensAsync())
                    return await response.Content.ReadFromJsonAsync<ErrorResponse>() ?? UnknownError;
                response = await http.SendAsync(factory());
            }
            if (response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<ErrorResponse>() ?? UnknownError;
        }
        catch
        {
            return UnknownError;
        }
    }

    private async Task<bool> TryRefreshTokensAsync()
    {
        if (string.IsNullOrWhiteSpace(auth.RefreshToken))
        {
            await auth.SignOutAsync();
            return false;
        }
        var (tokens, _) = await ExecuteAsync<LoginResponse>(() => Request(HttpMethod.Post, "/api/auth/refresh", new { token = auth.RefreshToken }), retry: false);
        if (tokens is null)
        {
            await auth.SignOutAsync();
            return false;
        }
        await auth.SetTokensAsync(tokens);
        return true;
    }

    // ── Auth (anonymous) ────────────────────────────────────────────────────

    public Task<(LoginResponse? data, ErrorResponse? error)> LoginAsync(string email, string password) =>
        ExecuteAsync<LoginResponse>(() => Request(HttpMethod.Post, "/api/auth/login", new { email, password }));

    public Task<ErrorResponse?> RegisterAsync(string userName, string email, string password, string? firstName = null, string? lastName = null, string? phoneNumber = null) =>
        ExecuteAsync(() => Request(HttpMethod.Post, "/api/auth/register", new { userName, email, password, firstName, lastName, phoneNumber }));

    public Task<ErrorResponse?> ForgotPasswordAsync(string email) =>
        ExecuteAsync(() => Request(HttpMethod.Post, "/api/auth/forgotPassword", new { email }));

    public Task<ErrorResponse?> ResetPasswordAsync(string email, string code, string newPassword) =>
        ExecuteAsync(() => Request(HttpMethod.Post, "/api/auth/resetPassword", new { email, code, newPassword }));

    public Task<ErrorResponse?> ConfirmEmailAsync(string email, string code) =>
        ExecuteAsync(() => Request(HttpMethod.Post, "/api/auth/confirmEmail", new { email, code }));

    // ── Auth (authenticated) ─────────────────────────────────────────────────

    public Task<(LoginResponse? data, ErrorResponse? error)> RefreshTokenAsync(string token) =>
        ExecuteAsync<LoginResponse>(() => Request(HttpMethod.Post, "/api/auth/refresh", new { token }), retry: false);

    public Task<(UserInfoResponse? data, ErrorResponse? error)> GetUserInfoAsync() =>
        ExecuteAsync<UserInfoResponse>(() => AuthorizedRequest(HttpMethod.Get, "/api/auth/info"));

    public Task<ErrorResponse?> LogoutAsync() =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/auth/logout"));

    // ── Roles ────────────────────────────────────────────────────────────────

    public Task<(PagingResponse<RoleResponse>? data, ErrorResponse? error)> GetRolesAsync(int page, int limit) =>
        ExecuteAsync<PagingResponse<RoleResponse>>(() => AuthorizedRequest(HttpMethod.Get, $"/api/roles?page={page}&limit={limit}"));

    public Task<ErrorResponse?> CreateRoleAsync(string name, string? description) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/roles", new { name, description }));

    public Task<ErrorResponse?> UpdateRoleAsync(Guid id, string name, string? description) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Put, $"/api/roles/{id}", new { name, description }));

    public Task<ErrorResponse?> DeleteRoleAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/roles/{id}"));

    // ── Users ────────────────────────────────────────────────────────────────

    public Task<(PagingResponse<UserResponse>? data, ErrorResponse? error)> GetUsersAsync(int page, int limit) =>
        ExecuteAsync<PagingResponse<UserResponse>>(() => AuthorizedRequest(HttpMethod.Get, $"/api/users?page={page}&limit={limit}"));

    public Task<ErrorResponse?> CreateUserAsync(string userName, string email, string password, string? firstName, string? lastName, string? phoneNumber, string[] roles) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Post, "/api/users", new { userName, email, password, firstName, lastName, phoneNumber, roles }));

    public Task<ErrorResponse?> UpdateUserAsync(Guid id, string email, string? firstName, string? lastName, string? phoneNumber, string[] roles) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Put, $"/api/users/{id}", new { email, firstName, lastName, phoneNumber, roles }));

    public Task<ErrorResponse?> DeleteUserAsync(Guid id) =>
        ExecuteAsync(() => AuthorizedRequest(HttpMethod.Delete, $"/api/users/{id}"));
}

public sealed record PagingResponse<T>(List<T> Items, int Page, int Limit, int Total);
public sealed record UserInfoResponse(Guid Id, string? UserName, string? Email, string? FirstName, string? LastName, string? PhoneNumber, string[] Roles);
public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType);
public sealed record RoleResponse(Guid Id, string Name, string? Description);
public sealed record UserResponse(Guid Id, string? UserName, string? Email, string? FirstName, string? LastName, string? PhoneNumber, bool EmailConfirmed, IEnumerable<string> Roles);
public sealed record ErrorResponse(string Code, string Message, Dictionary<string, string[]>? Errors = null);
