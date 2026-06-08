using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain;

namespace SourceBase.Application.Features.Auth;

public record LogoutRequest;

public record LogoutResponse(bool Success);

public class LogoutEndpoint : IEndpoint
{
    public const string Route = "auth/logout";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (LogoutHandler handler, CancellationToken ct) => handler.Handle(new LogoutRequest(), ct))
        .WithTags("Auth");
}

public class LogoutHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<LogoutRequest, LogoutResponse>
{
    public async Task<LogoutResponse> Handle(LogoutRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FindAsync([currentUser.UserId], ct) ?? throw new UnAuthorizedException("User not found");
        user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing tokens by changing the security stamp
        await dbContext.SaveChangesAsync(ct);
        return new LogoutResponse(true);
    }
}