using System.Net.Http.Headers;
using SourceBase.Web.Auth;

namespace SourceBase.Web.Http;

public class AuthHeaderHandler(BlazorAuthStateProvider authStateProvider) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && !string.IsNullOrWhiteSpace(authStateProvider.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authStateProvider.AccessToken);

        return base.SendAsync(request, cancellationToken);
    }
}
