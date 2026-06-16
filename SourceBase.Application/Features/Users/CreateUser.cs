using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Users;

public record CreateUserRequest(string UserName, string Email, string Password, string? FirstName, string? LastName, string? PhoneNumber, string[]? Roles);

public record CreateUserResponse(Guid Id);

public class CreateUserEndpoint : IEndpoint
{
    public const string Route = "users";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateUserRequest request, CreateUserHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class CreateUserHandler(IDbContext dbContext, ISecurityProvider securityProvider, IEmailHelper emailHelper, IOtpHelper otpHelper, INotificationService notificationService) : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserRequest request, CancellationToken ct)
    {
        var (confirmationCode, expiresOn) = otpHelper.Generate();
        var user = new UserEntity
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            OtpCode = confirmationCode,
            OtpCodeExpiresOn = expiresOn,
            PasswordHash = securityProvider.HashPassword(null!, request.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var normalizedRoles = request.Roles?.Normalize();
        if (normalizedRoles is not null)
        {
            var roles = await dbContext.Roles
                .Where(role => normalizedRoles.Contains(role.Name))
                .ToListAsync(ct);

            user.Roles = roles;
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(ct);

        await emailHelper.SendEmailAsync(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{confirmationCode}</b>");

        var adminUsers = await dbContext.Users
            .Where(u => u.Roles.Any(r => r.Name == AppRoles.Admin))
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var adminId in adminUsers)
            await notificationService.CreateAsync(new NotificationEntity
            {
                UserId = adminId,
                Event = NotificationEvent.GlobalNotificationEvent,
                Title = "New User Registered",
                Message = $"A new user {user.Email} has been registered.",
                Data = string.Empty,
            }, ct);

        return new CreateUserResponse(user.Id);
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.UserName)
            .MustAsync(async (name, ct) => !await dbContext.Users.AnyAsync(u => u.UserName == name, ct))
            .WithMessage("Username is already taken.")
            .When(x => !string.IsNullOrEmpty(x.UserName));
        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) => !await dbContext.Users.AnyAsync(u => u.Email == email, ct))
            .WithMessage("Email is already taken.")
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleForEach(x => x.Roles).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Roles)
            .MustAsync(async (roles, ct) =>
            {
                var normalized = roles!.Normalize();
                var count = await dbContext.Roles.CountAsync(r => r.Name != null && normalized.Contains(r.Name), ct);
                return count == normalized.Length;
            })
            .WithMessage("One or more specified roles do not exist.")
            .When(x => x.Roles is not null && x.Roles.Length > 0);
    }
}
