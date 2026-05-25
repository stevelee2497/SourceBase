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
        var (confirmationCode, expiresOn) = OtpHelper.Generate(appSettings.OtpTokenExpirationMinutes);
        var user = new UserEntity
        {
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpper(),
            UserName = request.UserName,
            NormalizedUserName = request.UserName.ToUpper(),
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
    public RegisterRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MustAsync(async (email, ct) =>
        {
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            return existingUser == null;
        }).WithMessage("Email is already taken.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
