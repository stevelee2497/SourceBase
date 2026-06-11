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
        if (await dbContext.Users.AnyAsync(u => u.UserName == request.UserName || u.Email == request.Email, ct))
            throw new BadRequestException("Username or email is already taken.");

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

            if (roles.Count != normalizedRoles.Length)
                throw new BadRequestException("One or more specified roles do not exist.");

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
            await notificationService.CreateAsync(adminId, "New User Registered", $"A new user {user.Email} has been registered.", ct);

        return new CreateUserResponse(user.Id);
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleForEach(x => x.Roles).NotEmpty().MaximumLength(256);
    }
}
