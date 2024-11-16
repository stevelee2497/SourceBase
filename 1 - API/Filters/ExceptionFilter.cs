using API.Models;
using Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters
{
    public class ExceptionFilter(IHostEnvironment hostEnvironment, ILogger<ExceptionFilter> logger) : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            logger.LogError(context.Exception, "Error on {env} at {time}: {message}", hostEnvironment.EnvironmentName, DateTime.Now, context.Exception.Message);

            context.Result = context.Exception switch
            {
                SystemApiException exception => new JsonResult(new SystemApiErrorModel { Code = exception.Code, Message = exception.Message, StackTrace = exception.StackTrace })
                {
                    StatusCode = exception.StatusCode
                },
                _ => new JsonResult(new SystemApiErrorModel { Code = "GENERIC CODE", Message = context.Exception.Message, StackTrace = context.Exception.StackTrace })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                },
            };
        }
    }
}
