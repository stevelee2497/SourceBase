using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Shared;

namespace SourceBase.Api.Middlewares;

public sealed class ErrorResponseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);

            await HandleAuthorizationErrorAsync(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleAuthorizationErrorAsync(HttpContext context)
    {
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

        await context.WriteResponseAsync(error);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        var error = exception switch
        {
            ApiException apiEx => apiEx,
            BadHttpRequestException { InnerException: JsonException jsonEx } => new ValidationException(errors: jsonEx.ExtractError()),
            _ => new ApiInternalException()
        };

        await context.WriteResponseAsync(error);
    }
}

public static class ErrorResponseMiddlewareUtilities
{
    public static IApplicationBuilder UseErrorResponse(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorResponseMiddleware>();
    }

    
    public static async Task WriteResponseAsync(this HttpContext context, ApiException? error)
    {
        if (error is null)
        {
            return;
        }

        if (error.Code != "VALIDATION ERROR")
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<ErrorResponseMiddleware>>();
            logger.LogWarning("Request failed for {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = error.StatusCode;
        context.Response.ContentLength = null;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = error.Code,
            Title = error.Message,
            Extensions = { [nameof(error.Errors)] = error.Errors }
        });
    }
    
    public static Dictionary<string, string[]> ExtractError(this JsonException jsonEx)
    {
        return new Dictionary<string, string[]> { [jsonEx.Path is { } p && p.StartsWith("$.", StringComparison.Ordinal) ? p[2..] : "body"] = ["The value is not valid."] };
    }
}