using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public record GooglePrepareConnectRequest;

public record GooglePrepareConnectResponse(string State);

public class GooglePrepareConnectEndpoint : IEndpoint
{
    public const string Route = "auth/google/connect/prepare";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (GooglePrepareConnectHandler handler, CancellationToken ct) => handler.Handle(new GooglePrepareConnectRequest(), ct))
        .WithTags("Auth");
}

public class GooglePrepareConnectHandler(ICacheService cacheService, ICurrentUser currentUser) : IRequestHandler<GooglePrepareConnectRequest, GooglePrepareConnectResponse>
{
    public async Task<GooglePrepareConnectResponse> Handle(GooglePrepareConnectRequest request, CancellationToken ct)
    {
        var state = Guid.NewGuid().ToString("N");
        await cacheService.SetAsync(CacheKeys.GoogleConnectState.WithState(state), currentUser.UserId.ToString()!, TimeSpan.FromMinutes(5), ct);
        return new GooglePrepareConnectResponse(state);
    }
}
