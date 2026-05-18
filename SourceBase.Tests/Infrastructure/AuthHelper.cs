using System.Net.Http.Headers;
using System.Net.Http.Json;
using SourceBase.Api.Features.Auth;

namespace SourceBase.Tests.Infrastructure;

public static class AuthHelper
{
    public static async Task AuthorizeAsync(this HttpClient client)
    {
        var token = await GetAccessTokenAsync(client, WebAppFactory.AdminEmail, WebAppFactory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<string> GetAccessTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body?.AccessToken ?? throw new InvalidOperationException("Access token not found in login response");
    }
}
