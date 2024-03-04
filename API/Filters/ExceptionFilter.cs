using API.Models;
using Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters
{
    public class ExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ExceptionFilter> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        public ExceptionFilter(IHostEnvironment hostEnvironment, ILogger<ExceptionFilter> logger)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Error on {env} at {time}: {message}", _hostEnvironment.EnvironmentName, DateTime.Now, context.Exception.Message);

            switch (context.Exception)
            {
                case SystemApiException exception:
                    context.Result = new JsonResult(new SystemApiErrorModel { Code = exception.Code, Message = exception.Message, StackTrace = exception.StackTrace })
                    {
                        StatusCode = exception.StatusCode
                    };
                    break;

                default:
                    context.Result = new JsonResult(new SystemApiErrorModel { Code = "GENERIC CODE", Message = context.Exception.Message, StackTrace = context.Exception.StackTrace })
                    {
                        StatusCode = StatusCodes.Status500InternalServerError
                    };
                    break;
            }
        }
    }
}
