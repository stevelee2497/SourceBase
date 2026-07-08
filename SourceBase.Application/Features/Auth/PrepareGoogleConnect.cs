using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public record PrepareGoogleConnectRequest;
public record PrepareGoogleConnectResponse(string State);

public class PrepareGoogleConnectEndpoint : IEndpoint
{
    public const string Route = "auth/google/connect/prepare";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (PrepareGoogleConnectHandler handler, CancellationToken ct) => handler.Handle(new PrepareGoogleConnectRequest(), ct))
        .WithTags("Auth");
}

public class PrepareGoogleConnectHandler(ICacheService cacheService, ICurrentUser currentUser) : IRequestHandler<PrepareGoogleConnectRequest, PrepareGoogleConnectResponse>
{
    public async Task<PrepareGoogleConnectResponse> Handle(PrepareGoogleConnectRequest request, CancellationToken ct)
    {
        var state = Guid.NewGuid().ToString("N");
        await cacheService.SetAsync(CacheKeys.GoogleConnectState.WithState(state), currentUser.UserId.ToString()!, TimeSpan.FromMinutes(5), ct);
        return new PrepareGoogleConnectResponse(state);
    }
}
