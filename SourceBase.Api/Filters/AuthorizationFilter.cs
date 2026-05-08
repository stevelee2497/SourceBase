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