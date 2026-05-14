using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Common;

namespace SourceBase.Api.Middlewares;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning(exception, "The response has already started, the exception filter will not overwrite it.");
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        var error = exception as ApiException ?? new ApiInternalException();

        logger.LogWarning(exception, "Request failed for {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.ContentLength = null;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = error.Code,
            Title = error.Message,
        });
    }
}