using System.Reflection;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace SourceBase.Api.Middlewares;

public class ValidationEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;

            // set id from route if the property has [property: FromRoute] attribute (used for update endpoints) => validator can check if the entity exists and belongs to the user
            var idProp = argument.GetType().GetProperties().FirstOrDefault(p => p.Name == "Id" && p.GetCustomAttribute<FromRouteAttribute>() is not null);
            if (idProp is not null
                && context.HttpContext.Request.RouteValues.TryGetValue("id", out var routeId)
                && Guid.TryParse(routeId?.ToString(), out var parsedId))
            {
                idProp.SetValue(argument, parsedId);
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (result.IsValid) continue;

            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => JsonNamingPolicy.CamelCase.ConvertName(g.Key), g => g.Select(e => e.ErrorMessage).ToArray());

            throw new Application.Shared.ValidationException(errors: errors);
        }

        return await next(context);
    }
}
