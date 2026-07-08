using Microsoft.AspNetCore.Authentication;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public class GoogleLoginEndpoint : IEndpoint
{
    public const string Route = "auth/google";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (HttpContext ctx) =>
        {
            var props = new AuthenticationProperties { RedirectUri = "/api/auth/google/complete" };
            return ctx.ChallengeAsync("Google", props);
        })
        .AllowAnonymous()
        .WithTags("Auth");
}
