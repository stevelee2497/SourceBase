using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Common;

namespace SourceBase.Api.Middlewares;

public sealed class AuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.HasStarted)
        {
            return;
        }

        var error = context.Response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => (ApiException)new UnAuthorizedException(),
            StatusCodes.Status403Forbidden => new ForbiddenException(),
            _ => null,
        };

        if (error is null)
        {
            return;
        }

        context.Response.ContentLength = null;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = error.Code,
            Title = error.Message
        });
    }
}