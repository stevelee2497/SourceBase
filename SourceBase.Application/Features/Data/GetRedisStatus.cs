using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Data;

public record GetRedisStatusRequest;

public record GetRedisStatusResponse(bool IsOnline);

public class GetRedisStatusEndpoint : IEndpoint
{
    public const string Route = "data/redis-status";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (GetRedisStatusHandler handler, CancellationToken ct) => handler.Handle(new GetRedisStatusRequest(), ct))
        .WithTags("Data");
}

public class GetRedisStatusHandler(ICacheService cacheService) : IRequestHandler<GetRedisStatusRequest, GetRedisStatusResponse>
{
    public async Task<GetRedisStatusResponse> Handle(GetRedisStatusRequest request, CancellationToken ct)
        => new(await cacheService.IsAvailableAsync());
}
