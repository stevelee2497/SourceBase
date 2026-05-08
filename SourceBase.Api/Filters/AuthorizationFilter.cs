using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using SourceBase.Application.Common;
using SourceBase.Infrastructure.DbContexts;
using System.Security.Claims;

namespace SourceBase.Api.Filters;

public class AuthorizationFilter(ApplicationDbContext dbContext) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        //continue if allow anonymous
        if (context.Filters.Any(item => item is IAllowAnonymousFilter) || !context.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>().Any()) return;

        if (await IsUserAuthorized(context.HttpContext.User, context.HttpContext.RequestAborted)) return;
      
        var exception = new UnAuthorizedException();
        context.Result = new JsonResult(new SystemApiErrorModel(exception.Code, exception.Message, null, null))
        {
            StatusCode = exception.StatusCode
        };
    }

    private async Task<bool> IsUserAuthorized(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var principal = user.Identity;
        if (principal is not { IsAuthenticated: true })
            return false;

        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return false;

        var existingUser = await dbContext.Users.FindAsync([userId], cancellationToken);
        if (existingUser == null)
            return false;

        if (!existingUser.EmailConfirmed)
            return false;

        if (existingUser.LockoutEnabled && existingUser.LockoutEnd > DateTimeOffset.UtcNow)
            return false;

        var securityStamp = user.FindFirst("AspNet.Identity.SecurityStamp")?.Value;
        return string.IsNullOrWhiteSpace(securityStamp) || string.Equals(existingUser.SecurityStamp, securityStamp, StringComparison.Ordinal);
    }
}