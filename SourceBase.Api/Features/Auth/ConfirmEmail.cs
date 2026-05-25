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
    public const string Route = "auth/confirmEmail";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] ConfirmEmailRequest request, ConfirmEmailHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ConfirmEmailHandler(UserManager<UserEntity> userManager) : IRequestHandler<ConfirmEmailRequest, ConfirmEmailResponse>
{
    public async Task<ConfirmEmailResponse> Handle(ConfirmEmailRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new UnAuthorizedException();
        if (user.OtpCode != request.Code || user.OtpCodeExpiresOn is null || user.OtpCodeExpiresOn <= DateTime.UtcNow)
            throw new UnAuthorizedException("Invalid or expired code");

        user.OtpCode = null;
        user.OtpCodeExpiresOn = null;
        user.EmailConfirmed = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new BadRequestException(updateResult.Errors.First().Description);

        var addRoleResult = await userManager.AddToRoleAsync(user, AppRoles.User);
        if (!addRoleResult.Succeeded)
            throw new BadRequestException(addRoleResult.Errors.First().Description);

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
