using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record ConfirmEmailRequest(string Email, string Code);

public record ConfirmEmailResponse(bool Success);

public class ConfirmEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/confirmEmail", ([FromBody] ConfirmEmailRequest request, ConfirmEmailHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ConfirmEmailHandler(UserManager<UserEntity> userManager) : IRequestHandler<ConfirmEmailRequest, ConfirmEmailResponse>
{
    public async Task<ConfirmEmailResponse> Handle(ConfirmEmailRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new UnAuthorizedException();
        if (user.OtpCode != request.Code)
        {
            throw new UnAuthorizedException();
        }

        user.OtpCode = null;
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);
        await userManager.AddToRoleAsync(user, AppRoles.User);
        return new ConfirmEmailResponse(true);
    }
}

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
