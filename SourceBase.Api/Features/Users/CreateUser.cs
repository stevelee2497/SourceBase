using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Users;

public record CreateUserRequest(string UserName, string Email, string Password, string? FirstName, string? LastName, string? PhoneNumber, string[]? Roles);

public record CreateUserResponse(Guid Id);

public class CreateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/users", ([FromBody] CreateUserRequest request, CreateUserHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class CreateUserHandler(
    UserManager<UserEntity> userManager,
    IEmailHelper emailHelper,
    AppSettings appSettings) : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserRequest request, CancellationToken ct)
    {
        var (confirmationCode, expiresOn) = OtpHelper.Generate(appSettings.OtpTokenExpirationMinutes);
        var user = new UserEntity
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            OtpCode = confirmationCode,
            OtpCodeExpiresOn = expiresOn,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new BadRequestException(createResult.Errors.First().Description);

        var normalizedRoles = request.Roles?.Normalize();
        if (normalizedRoles is not null)
        {
            var addRolesResult = await userManager.AddToRolesAsync(user, normalizedRoles);
            if (!addRolesResult.Succeeded)
                throw new BadRequestException(addRolesResult.Errors.First().Description);
        }

        await emailHelper.SendEmailAsync(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{confirmationCode}</b>");

        return new CreateUserResponse(user.Id);
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(RoleManager<RoleEntity> roleManager)
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleForEach(x => x.Roles).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Roles).CustomAsync(async (roles, context, ct) =>
        {
            if (roles is null)
                return;

            foreach (var role in roles.Normalize())
            {
                if (await roleManager.RoleExistsAsync(role) is false)
                    context.AddFailure(nameof(CreateUserRequest.Roles), $"Role '{role}' does not exist");
            }
        });
    }
}
