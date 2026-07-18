using Microsoft.AspNetCore.Authentication;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public class GoogleConnectEndpoint : IEndpoint
{
    public const string Route = "auth/google/connect";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, async (string state, HttpContext ctx, ICacheService cacheService, CancellationToken ct) =>
        {
            var stored = await cacheService.GetAsync<string>(CacheKeys.GoogleConnectState.WithState(state), ct);
            if (stored is null) return Results.BadRequest("Invalid or expired state token.");

            var props = new AuthenticationProperties { RedirectUri = "/api/auth/google/complete" };
            props.Items["connect_state"] = state;
            await ctx.ChallengeAsync("Google", props);
            return Results.Empty;
        })
        .AllowAnonymous()
        .WithTags("Auth");
}
