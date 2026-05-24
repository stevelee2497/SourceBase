using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record RegisterRequest(string UserName, string Email, string Password);

public record RegisterResponse(Guid Id);

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/register", ([FromBody] RegisterRequest request, RegisterHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class RegisterHandler(UserManager<UserEntity> userManager, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<RegisterRequest, RegisterResponse>
{
    public async Task<RegisterResponse> Handle(RegisterRequest request, CancellationToken ct)
    {
        var confirmationCode = OtpHelper.Generate();
        var user = new UserEntity
        {
            Email = request.Email,
            UserName = request.UserName,
            OtpCode = confirmationCode,
            OtpCodeExpiresOn = OtpHelper.GetExpiresOn(appSettings.OtpTokenExpirationMinutes),
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new BadRequestException(createResult.Errors.First().Description);

        await emailHelper.SendEmailAsync(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{confirmationCode}</b>");

        return new RegisterResponse(user.Id);
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
