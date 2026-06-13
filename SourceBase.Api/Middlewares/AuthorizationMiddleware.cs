using Microsoft.AspNetCore.Authorization;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Api.Middlewares;

public class AuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IDbContext dbContext)
    {
        if (!await IsRequestValidAsync(context, dbContext))
        {
            throw new UnAuthorizedException("User is not authorized");
        }

        await next(context);
    }

    private static async Task<bool> IsRequestValidAsync(HttpContext context, IDbContext dbContext)
    {
        // If the endpoint is null, it means that the request does not match any route, so we can allow it to pass through and let the routing middleware handle it.
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
            return true;

        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            return true;

        if (context.User.Identity is not { IsAuthenticated: true })
            return false;

        var user = await dbContext.Users.FindAsync([context.User.UserId], context.RequestAborted);
        if (user is null || !user.EmailConfirmed || !string.Equals(user.SecurityStamp, context.User.SecurityStamp, StringComparison.Ordinal))
            return false;

        return true;
    }
}

public static class AuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthorizationMiddleware>();
    }
}
