using Microsoft.AspNetCore.Authorization;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain;

namespace SourceBase.Api.Middlewares;

public class AuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IDbContext dbContext)
    {
        if (await IsUserValidAsync(context, dbContext) is false)
        {
            throw new UnAuthorizedException("User is not authorized");
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
