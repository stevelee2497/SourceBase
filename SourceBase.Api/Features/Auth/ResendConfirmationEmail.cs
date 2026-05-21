using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class ResendConfirmationEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/resendConfirmationEmail", ([FromBody] ResendConfirmationEmailRequest request, IRequestHandler<ResendConfirmationEmailRequest, NoContent> handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ResendConfirmationEmailHandler(UserManager<UserEntity> userManager, IEmailHelper emailHelper) : IRequestHandler<ResendConfirmationEmailRequest, NoContent>
{
    public async Task<NoContent> Handle(ResendConfirmationEmailRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        if (user.EmailConfirmed)
            throw new BadRequestException("Email already confirmed");

        var otp = OtpHelper.Generate();
        user.OtpCode = otp;
        await userManager.UpdateAsync(user);
        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Your confirmation code is: <b>{otp}</b>");
        return TypedResults.NoContent();
    }
}

public record ResendConfirmationEmailRequest(string Email);

public class ResendConfirmationEmailRequestValidator : AbstractValidator<ResendConfirmationEmailRequest>
{
    public ResendConfirmationEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
