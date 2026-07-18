using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public class GoogleExchangeEndpoint : IEndpoint
{
    public const string Route = "auth/google/exchange";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (string code, GoogleExchangeHandler handler, CancellationToken ct) => handler.Handle(code, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class GoogleExchangeHandler(IDbContext dbContext, ICacheService cacheService, ISecurityProvider securityProvider, IHttpContextAccessor httpContextAccessor) : IRequestHandler<string, EmptyHttpResult>
{
    public async Task<EmptyHttpResult> Handle(string code, CancellationToken ct)
    {
        var userIdStr = await cacheService.GetAsync<string>(CacheKeys.GoogleExchange.WithCode(code), ct);
        if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            throw new UnAuthorizedException("Invalid or expired exchange code.");

        await cacheService.RemoveAsync(CacheKeys.GoogleExchange.WithCode(code), ct);

        var user = await dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnAuthorizedException("User not found.");

        var claimsPrincipal = securityProvider.CreateClaimsPrincipal(user);
        await httpContextAccessor.HttpContext!.SignInAsync(Constants.BearerScheme, claimsPrincipal);
        return TypedResults.Empty;
    }
}
