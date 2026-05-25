using Microsoft.AspNetCore.Authorization;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Middlewares;

public class AuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IDbContext dbContext)
    {
        if (await IsUserValidAsync(context, dbContext) is false)
        {
            throw new UnAuthorizedException();
        }

        await next(context);
    }

    private static async Task<bool> IsUserValidAsync(HttpContext context, IDbContext dbContext)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            return true;

        if (context.User.Identity is not { IsAuthenticated: true })
            return false;

        var user = await dbContext.Users.FindAsync([context.User.UserId], context.RequestAborted);
        if (user is null || !user.EmailConfirmed || (user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow) || !string.Equals(user.SecurityStamp, context.User.SecurityStamp, StringComparison.Ordinal))
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
