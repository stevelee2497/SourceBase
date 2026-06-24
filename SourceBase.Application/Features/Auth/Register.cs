using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public record RegisterRequest(string UserName, string Email, string Password, string FirstName, string LastName);

public record RegisterResponse(Guid Id);

public class RegisterEndpoint : IEndpoint
{
    public const string Route = "auth/register";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] RegisterRequest request, RegisterHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .RequireRateLimiting(Constants.StrictRateLimitPolicy)
        .WithTags("Auth");
}

public class RegisterHandler(IDbContext dbContext, ISecurityProvider securityProvider, IMessageQueuePublisher messageQueuePublisher, IOtpHelper otpHelper) : IRequestHandler<RegisterRequest, RegisterResponse>
{
    public async Task<RegisterResponse> Handle(RegisterRequest request, CancellationToken ct)
    {
        if (await dbContext.Users.AnyAsync(u => u.UserName == request.UserName || u.Email == request.Email, ct))
            throw new BadRequestException("Username or email is already taken.");

        var (confirmationCode, expiresOn) = otpHelper.Generate();
        var user = new UserEntity
        {
            Email = request.Email,
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            OtpCode = confirmationCode,
            OtpCodeExpiresOn = expiresOn,
            PasswordHash = securityProvider.HashPassword(null!, request.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        dbContext.Users.Add(user);
        dbContext.Emails.Add(new EmailEntity(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{user.OtpCode}</b>"));
        await dbContext.SaveChangesAsync(ct);

        await messageQueuePublisher.PublishAsync("email", new EmailMessage(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{user.OtpCode}</b>"), ct);

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
