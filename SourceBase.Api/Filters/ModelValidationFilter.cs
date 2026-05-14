using System.ComponentModel.DataAnnotations;

namespace SourceBase.Api.Filters;

public class ModelValidationFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var errors = new Dictionary<string, string>();

        foreach (var argument in context.Arguments)
        {
            if (argument == null)
            {
                continue;
            }

            var argumentType = argument.GetType();
            if (IsSimpleType(argumentType))
            {
                continue;
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(argument);
            if (Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true))
            {
                continue;
            }

            foreach (var validationResult in validationResults)
            {
                var key = validationResult.MemberNames.FirstOrDefault() ?? argumentType.Name;
                errors[key] = validationResult.ErrorMessage ?? "invalid value";
            }
        }

        if (errors.Count == 0)
        {
            return next(context);
        }

        return ValueTask.FromResult<object?>(Results.Json(new SystemApiErrorModel("INVALID FIELDS", "error on model binding validation", null, errors), statusCode: StatusCodes.Status400BadRequest));
    }

    private static bool IsSimpleType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        if (underlyingType.IsPrimitive || underlyingType.IsEnum)
        {
            return true;
        }

        return underlyingType == typeof(string)
            || underlyingType == typeof(Guid)
            || underlyingType == typeof(DateOnly)
            || underlyingType == typeof(DateTime)
            || underlyingType == typeof(DateTimeOffset)
            || underlyingType == typeof(TimeOnly)
            || underlyingType == typeof(decimal)
            || underlyingType == typeof(CancellationToken);
    }
}
