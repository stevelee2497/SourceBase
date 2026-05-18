using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SourceBase.Api.Features.Auth;

namespace SourceBase.Tests.Infrastructure;

public static class Utilities
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static Task<T?> ReadFromJsonAsync<T>(this HttpContent content, CancellationToken ct = default)
    {
        return content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string? requestUri, T value, CancellationToken ct = default)
    {
        return client.PostAsJsonAsync(requestUri, value, JsonOptions, ct);
    }

    public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string? requestUri, T value, CancellationToken ct = default)
    {
        return client.PutAsJsonAsync(requestUri, value, JsonOptions, ct);
    }

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
