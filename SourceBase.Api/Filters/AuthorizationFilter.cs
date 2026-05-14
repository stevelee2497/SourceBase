using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.DbContexts;

namespace SourceBase.Api.Filters;

public class AuthorizationFilter(ApplicationDbContext dbContext) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            return await next(context);
        }

        if (await IsUserAuthorized(context.HttpContext.User, context.HttpContext.RequestAborted))
        {
            return await next(context);
        }
      
        var exception = new UnAuthorizedException();
        return Results.Json(new SystemApiErrorModel(exception.Code, exception.Message, null, null), statusCode: exception.StatusCode);
    }

    private async Task<bool> IsUserAuthorized(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (user.Identity is not { IsAuthenticated: true } || Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) is false)
            return false;

        var existingUser = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (existingUser == null || existingUser.EmailConfirmed is false || existingUser.LockoutEnabled && existingUser.LockoutEnd > DateTimeOffset.UtcNow)
            return false;

        var securityStamp = user.FindFirst("AspNet.Identity.SecurityStamp")?.Value;
        if (string.IsNullOrWhiteSpace(securityStamp) || !string.Equals(existingUser.SecurityStamp, securityStamp, StringComparison.Ordinal))
            return false;

        return true;
    }
}