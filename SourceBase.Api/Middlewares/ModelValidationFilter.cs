
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class ModelValidationFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        var errors  = GetModelStateInvalidInfo(context);
        context.Result = new JsonResult(new ProblemDetails
        {
            Title = "The request is invalid.",
            Detail = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Extensions = { ["errors"] = errors }
        });
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
