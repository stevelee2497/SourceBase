using SourceBase.Api.Common;

namespace SourceBase.Api.Filters;

public class ExceptionFilter(RequestDelegate next, IHostEnvironment hostEnvironment, ILogger<ExceptionFilter> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error on {env} at {time}: {message}", hostEnvironment.EnvironmentName, DateTime.Now, exception.Message);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var (statusCode, error) = exception switch
            {
                UnAuthorizedException unauthorizedException => (unauthorizedException.StatusCode, new SystemApiErrorModel(unauthorizedException.Code, unauthorizedException.Message, null, null)),
                ApiException apiException => (apiException.StatusCode, new SystemApiErrorModel(apiException.Code, apiException.Message, apiException.StackTrace, null)),
                _ => (StatusCodes.Status500InternalServerError, new SystemApiErrorModel("GENERIC CODE", exception.Message, exception.StackTrace, null))
            };

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(error);
        }
    }
}

public record SystemApiErrorModel(string Code, string Message, string? StackTrace, Dictionary<string, string>? Details);