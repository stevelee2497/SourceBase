using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public record GetUserInfoRequest;

public record GetUserInfoResponse(Guid Id, string? UserName, string? Email, bool EmailConfirmed, string? FirstName, string? LastName, string? PhoneNumber, string? AvatarUrl, Guid? DefaultTodoListId, string[] Roles);

public class GetUserInfoEndpoint : IEndpoint
{
    public const string Route = "auth/info";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (GetUserInfoHandler handler, CancellationToken ct) => handler.Handle(new GetUserInfoRequest(), ct))
        .WithTags("Auth");
}

public class GetUserInfoHandler(ICurrentUser currentUser, IDbContext dbContext, ICacheService cacheService) : IRequestHandler<GetUserInfoRequest, GetUserInfoResponse>
{
    public static string CacheKey(Guid userId) => $"user-info:{userId}";

    public async Task<GetUserInfoResponse> Handle(GetUserInfoRequest request, CancellationToken ct)
    {
        var cached = await cacheService.GetAsync<GetUserInfoResponse>(CacheKey(currentUser.UserId), ct);
        if (cached is not null) return cached;

        var user = await dbContext.Users.FindAsync([currentUser.UserId], ct);
        var result = new GetUserInfoResponse(
            Id: user!.Id,
            UserName: user.UserName,
            Email: user.Email,
            EmailConfirmed: user.EmailConfirmed,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            AvatarUrl: user.AvatarUrl,
            DefaultTodoListId: user.DefaultTodoListId,
            Roles: currentUser.Roles
        );
        await cacheService.SetAsync(CacheKey(currentUser.UserId), result, TimeSpan.FromMinutes(30), ct);
        return result;
    }
}
