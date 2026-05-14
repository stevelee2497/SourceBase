using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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

        var error = exception switch
        {
            ApiException apiEx => new ProblemDetails
            {
                Type = apiEx.Code,
                Title = apiEx.Message,
            },
            BadHttpRequestException { InnerException: JsonException jsonEx } => new ProblemDetails
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = ExtractError(jsonEx) }
            },
            _ => new ProblemDetails
            {
                Type = "GENERIC CODE",
                Title = "Something went wrong",
            }
        };


        logger.LogWarning(exception, "Request failed for {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.ContentLength = null;
        await context.Response.WriteAsJsonAsync(error);
    }

    private static Dictionary<string, string[]> ExtractError(JsonException jsonEx)
    {
        return new Dictionary<string, string[]> { [jsonEx.Path is { } p && p.StartsWith("$.", StringComparison.Ordinal) ? p[2..] : "body"] = ["The value is not valid."] };
    }
}