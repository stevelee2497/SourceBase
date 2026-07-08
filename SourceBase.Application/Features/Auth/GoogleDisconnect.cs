using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public record DisconnectGoogleRequest;
public record DisconnectGoogleResponse(bool Success);

public class DisconnectGoogleEndpoint : IEndpoint
{
    public const string Route = "auth/google/disconnect";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (DisconnectGoogleHandler handler, CancellationToken ct) => handler.Handle(new DisconnectGoogleRequest(), ct))
        .WithTags("Auth");
}

public class DisconnectGoogleHandler(IDbContext dbContext, ICurrentUser currentUser, ICacheService cacheService) : IRequestHandler<DisconnectGoogleRequest, DisconnectGoogleResponse>
{
    public async Task<DisconnectGoogleResponse> Handle(DisconnectGoogleRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        if (user.PasswordHash is null)
            throw new BadRequestException("Cannot disconnect Google when no password is set.");

        user.GoogleId = null;
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.UserInfo.WithId(currentUser.UserId), ct);

        return new DisconnectGoogleResponse(true);
    }
}
