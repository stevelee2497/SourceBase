using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Api.Features.Users;

public record UpdateUserRequest([property: SwaggerIgnore] Guid Id, string Email, string? FirstName, string? LastName, string? PhoneNumber, string[]? Roles);

public record UpdateUserResponse(Guid Id);

public class UpdateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut("/users/{id:guid}", (Guid id, [FromBody] UpdateUserRequest body, UpdateUserHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class UpdateUserHandler(
    UserManager<UserEntity> userManager,
    IEmailHelper emailHelper,
    AppSettings appSettings) : IRequestHandler<UpdateUserRequest, UpdateUserResponse>
{
    public async Task<UpdateUserResponse> Handle(UpdateUserRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.Id.ToString()) ?? throw new NotFoundException();
        var normalizedRoles = request.Roles?.Normalize();

        var trimmedEmail = request.Email;
        var emailChanged = string.Equals(user.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase) is false;

        user.Email = trimmedEmail;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        if (emailChanged)
        {
            var (confirmationCode, expiresOn) = OtpHelper.Generate(appSettings.OtpTokenExpirationMinutes);
            user.EmailConfirmed = false;
            user.OtpCode = confirmationCode;
            user.OtpCodeExpiresOn = expiresOn;
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new BadRequestException(updateResult.Errors.First().Description);

        var rolesChanged = false;
        if (normalizedRoles is not null)
        {
            var existingRoles = await userManager.GetRolesAsync(user);
            var rolesToRemove = existingRoles.Except(normalizedRoles, StringComparer.OrdinalIgnoreCase).ToArray();
            var rolesToAdd = normalizedRoles.Except(existingRoles, StringComparer.OrdinalIgnoreCase).ToArray();

            if (rolesToRemove.Length > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                    throw new BadRequestException(removeResult.Errors.First().Description);
            }

            if (rolesToAdd.Length > 0)
            {
                var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                    throw new BadRequestException(addResult.Errors.First().Description);
            }

            rolesChanged = rolesToRemove.Length > 0 || rolesToAdd.Length > 0;
        }

        if (emailChanged || rolesChanged)
        {
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                throw new BadRequestException(stampResult.Errors.First().Description);
        }

        if (emailChanged)
            await emailHelper.SendEmailAsync(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{user.OtpCode}</b>");

        return new UpdateUserResponse(user.Id);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator(RoleManager<RoleEntity> roleManager)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
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
                    context.AddFailure(nameof(UpdateUserRequest.Roles), $"Role '{role}' does not exist");
            }
        });
    }
}
