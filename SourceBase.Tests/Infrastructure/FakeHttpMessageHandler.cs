using System.Net;

namespace SourceBase.Tests.Infrastructure;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;
    private readonly Exception? _exception;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    public FakeHttpMessageHandler(Exception exception)
    {
        _exception = exception;
        _statusCode = HttpStatusCode.OK;
        _content = string.Empty;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (_exception is not null) throw _exception;
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content),
        });
    }
}

public class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}
