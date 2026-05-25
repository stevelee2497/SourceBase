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
    public const string Route = "auth/register";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] RegisterRequest request, RegisterHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class RegisterHandler(UserManager<UserEntity> userManager, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<RegisterRequest, RegisterResponse>
{
    public async Task<RegisterResponse> Handle(RegisterRequest request, CancellationToken ct)
    {
        var (confirmationCode, expiresOn) = OtpHelper.Generate(appSettings.OtpTokenExpirationMinutes);
        var user = new UserEntity
        {
            Email = request.Email,
            UserName = request.UserName,
            OtpCode = confirmationCode,
            OtpCodeExpiresOn = expiresOn,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new BadRequestException(createResult.Errors.First().Description);

        await emailHelper.SendEmailAsync(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{user.OtpCode}</b>");

        return new RegisterResponse(user.Id);
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator(UserManager<UserEntity> userManager)
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MustAsync(async (email, ct) =>
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            return existingUser == null;
        }).WithMessage("Email is already taken.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
