using Core.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters;

public class ApiAuthorizationFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        //continue if allow anonymous
        if (context.Filters.Any(item => item is IAllowAnonymousFilter) || !context.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>().Any()) return Task.CompletedTask;

        var principal = context.HttpContext.User.Identity;
        if (principal is not { IsAuthenticated: true })
            throw new UnAuthorizedException();

        return Task.CompletedTask;
    }
}