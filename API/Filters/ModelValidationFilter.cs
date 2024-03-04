using API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters
{
    public class ModelValidationFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ModelState.IsValid) return;

            // Response Result
            context.Result = new JsonResult(new SystemApiErrorModel { Code = "INVALID FIELDS", Message = "Error on model binding validation", Details = GetModelStateInvalidInfo(context) })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
        private Dictionary<string, object> GetModelStateInvalidInfo(ActionContext context)
        {
            var errors = new Dictionary<string, object>();

            foreach (var keyValueState in context.ModelState)
            {
                var error = string.Join(", ", keyValueState.Value.Errors.Select(x => x.ErrorMessage));

                errors.Add(keyValueState.Key, error);
            }

            return errors;
        }
    }
}
