using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Shared;

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
                Status = apiEx.StatusCode
            },
            BadHttpRequestException { InnerException: JsonException jsonEx } => new ProblemDetails
            {
                Type = "VALIDATION ERROR",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Extensions = { ["errors"] = ExtractError(jsonEx) }
            },
            _ => new ProblemDetails
            {
                Type = "GENERIC CODE",
                Title = "Something went wrong",
                Status = StatusCodes.Status500InternalServerError
            }
        };

        if (error.Type != "VALIDATION ERROR")
        {
            logger.LogWarning(exception, "Request failed for {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = error.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentLength = null;
        error.Status = null; // Status is already set in the response, no need to include it in the body
        
        await context.Response.WriteAsJsonAsync(error);
    }

    private static Dictionary<string, string[]> ExtractError(JsonException jsonEx)
    {
        return new Dictionary<string, string[]> { [jsonEx.Path is { } p && p.StartsWith("$.", StringComparison.Ordinal) ? p[2..] : "body"] = ["The value is not valid."] };
    }
}