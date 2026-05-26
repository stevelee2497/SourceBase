using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Api.Features.Users;

public record UpdateUserRequest([property: SwaggerIgnore] Guid Id, string Email, string? FirstName, string? LastName, string? PhoneNumber, string[]? Roles);

public record UpdateUserResponse(Guid Id);

public class UpdateUserEndpoint : IEndpoint
{
    public const string Route = "users/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, (Guid id, [FromBody] UpdateUserRequest body, UpdateUserHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class UpdateUserHandler(
    IDbContext dbContext,
    IEmailHelper emailHelper,
    AppSettings appSettings) : IRequestHandler<UpdateUserRequest, UpdateUserResponse>
{
    public async Task<UpdateUserResponse> Handle(UpdateUserRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct) ?? throw new NotFoundException();
        var normalizedRoles = request.Roles?.Normalize().Select(role => role.ToUpper()).ToArray();

        var trimmedEmail = request.Email;
        if (await dbContext.Users.AnyAsync(u => u.Id != request.Id && u.NormalizedEmail == trimmedEmail.ToUpper(), ct))
            throw new BadRequestException("Email is already taken.");

        var emailChanged = string.Equals(user.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase) is false;

        user.Email = trimmedEmail;
        user.NormalizedEmail = trimmedEmail.ToUpper();
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

        var rolesChanged = false;
        if (normalizedRoles is not null)
        {
            var existingRoles = user.Roles
                .Where(role => !string.IsNullOrWhiteSpace(role.Name))
                .ToDictionary(
                    role => role.NormalizedName ?? role.Name!.ToUpper(),
                    role => role,
                    StringComparer.Ordinal);
            var rolesToRemove = existingRoles
                .Where(role => normalizedRoles.Contains(role.Key, StringComparer.Ordinal) is false)
                .Select(role => role.Value)
                .ToArray();
            var rolesToAdd = normalizedRoles.Except(existingRoles.Keys, StringComparer.Ordinal).ToArray();

            if (rolesToRemove.Length > 0)
            {
                foreach (var role in rolesToRemove)
                    user.Roles.Remove(role);
            }

            if (rolesToAdd.Length > 0)
            {
                var roles = await dbContext.Roles
                    .Where(role => role.NormalizedName != null && rolesToAdd.Contains(role.NormalizedName))
                    .ToListAsync(ct);

                if (roles.Count != rolesToAdd.Length)
                    throw new BadRequestException("One or more specified roles do not exist.");

                foreach (var role in roles)
                    user.Roles.Add(role);
            }

            rolesChanged = rolesToRemove.Length > 0 || rolesToAdd.Length > 0;
        }

        if (emailChanged || rolesChanged)
            user.SecurityStamp = Guid.NewGuid().ToString();

        await dbContext.SaveChangesAsync(ct);

        if (emailChanged)
            await emailHelper.SendEmailAsync(user.Email!, "Confirm your email", $"Your confirmation code is: <b>{user.OtpCode}</b>");

        return new UpdateUserResponse(user.Id);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleForEach(x => x.Roles).NotEmpty().MaximumLength(256);
    }
}
