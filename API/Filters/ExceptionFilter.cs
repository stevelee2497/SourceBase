using Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters;

public class ExceptionFilter(IHostEnvironment hostEnvironment, ILogger<ExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        logger.LogError(context.Exception, "Error on {env} at {time}: {message}", hostEnvironment.EnvironmentName, DateTime.Now, context.Exception.Message);

        switch (context.Exception)
        {
            case BaseException exception:
                context.Result = new JsonResult(new SystemApiErrorModel(exception.Code, exception.Message, exception.StackTrace, null)) { StatusCode = exception.StatusCode };
                break;
            default:
                context.Result =
                    new JsonResult(new SystemApiErrorModel("GENERIC CODE", context.Exception.Message, context.Exception.StackTrace, null)) { StatusCode = StatusCodes.Status500InternalServerError };
                break;
        }
    }
}

public record SystemApiErrorModel(string Code, string Message, string? StackTrace, Dictionary<string, object>? Details);