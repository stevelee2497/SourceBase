using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

public class RegisterHandler(IDbContext dbContext, ISecurityProvider securityProvider, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<RegisterRequest, RegisterResponse>
{
    public async Task<RegisterResponse> Handle(RegisterRequest request, CancellationToken ct)
    {
        if (await dbContext.Users.AnyAsync(u => u.UserName == request.UserName || u.Email == request.Email, ct))
            throw new BadRequestException("Username or email is already taken.");

        var (confirmationCode, expiresOn) = OtpHelper.Generate(appSettings.OtpTokenExpirationMinutes);
        var user = new UserEntity
        {
            Email = request.Email,
            UserName = request.UserName,
            OtpCode = confirmationCode,
            OtpCodeExpiresOn = expiresOn,
            PasswordHash = securityProvider.HashPassword(null!, request.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(ct);

        await emailHelper.SendEmailAsync(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{user.OtpCode}</b>");

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
