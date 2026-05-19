using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class ConfirmEmail : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPost("/auth/confirmEmail", Handler).WithTags("Auth").AllowAnonymous();

    private async Task<NoContent> Handler([FromBody] ConfirmEmailRequest request, UserManager<UserEntity> userManager, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new UnAuthorizedException();
        if (user.OtpCode != request.Code)
        {
            throw new UnAuthorizedException();
        }

        user.OtpCode = null;
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
        await userManager.AddToRoleAsync(user, Roles.User);
        return TypedResults.NoContent();
    }
}

public record ConfirmEmailRequest(string Email, string Code);

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
