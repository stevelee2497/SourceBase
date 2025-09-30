using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SourceBase.Api.Filters;

public class ModelValidationFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        // Response Result
        context.Result = new JsonResult(new SystemApiErrorModel("INVALID FIELDS", "error on model binding validation", null, GetModelStateInvalidInfo(context)))
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }

    private static Dictionary<string, string> GetModelStateInvalidInfo(ActionContext context)
    {
        var errors = new Dictionary<string, string>();

        foreach (var keyValueState in context.ModelState)
        {
            var error = string.Join(", ", keyValueState.Value.Errors.Select(x => x.ErrorMessage));

            errors.Add(keyValueState.Key, error);
        }

        if (errors.Any(x => x.Value.Contains("The JSON value could not be converted")))
        {
            return errors
                .Where(x => x.Value.Contains("The JSON value could not be converted"))
                .ToDictionary(x => x.Key.Replace("$.", ""), x => "enum value is not valid");
        }

        return errors;
    }
}
