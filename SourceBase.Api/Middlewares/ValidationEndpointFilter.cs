using System.Text.Json;
using FluentValidation;

namespace SourceBase.Api.Middlewares;

public class ValidationEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (result.IsValid) continue;

            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => JsonNamingPolicy.CamelCase.ConvertName(g.Key), g => g.Select(e => e.ErrorMessage).ToArray());

            throw new Domain.ValidationException(errors: errors);
        }

        return await next(context);
    }
}
