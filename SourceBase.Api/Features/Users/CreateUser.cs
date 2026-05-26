using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Users;

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

public class CreateUserHandler(
    IDbContext dbContext,
    ISecurityProvider securityProvider,
    IEmailHelper emailHelper,
    AppSettings appSettings) : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserRequest request, CancellationToken ct)
    {
        if (await dbContext.Users.AnyAsync(u => u.NormalizedUserName == request.UserName.ToUpper() || u.NormalizedEmail == request.Email.ToUpper(), ct))
            throw new BadRequestException("Username or email is already taken.");

        var (confirmationCode, expiresOn) = OtpHelper.Generate(appSettings.OtpTokenExpirationMinutes);
        var user = new UserEntity
        {
            UserName = request.UserName,
            NormalizedUserName = request.UserName.ToUpper(),
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpper(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            OtpCode = confirmationCode,
            OtpCodeExpiresOn = expiresOn,
            PasswordHash = securityProvider.HashPassword(null!, request.Password),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

        var normalizedRoles = request.Roles?.Normalize().Select(role => role.ToUpper()).ToArray();
        if (normalizedRoles is not null)
        {
            var roles = await dbContext.Roles
                .Where(role => normalizedRoles.Contains(role.NormalizedName))
                .ToListAsync(ct);

            if (roles.Count != normalizedRoles.Length)
                throw new BadRequestException("One or more specified roles do not exist.");

            user.Roles = roles;
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(ct);

        await emailHelper.SendEmailAsync(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{confirmationCode}</b>");

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
