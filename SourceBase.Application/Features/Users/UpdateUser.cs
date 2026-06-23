using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Features.Auth;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Users;

public record UpdateUserRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string Email, string? FirstName, string? LastName, string? PhoneNumber, string? AvatarUrl, string[]? Roles);

public record UpdateUserResponse(Guid Id);

public class UpdateUserEndpoint : IEndpoint
{
    public const string Route = "users/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, ([FromBody] UpdateUserRequest body, UpdateUserHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class UpdateUserHandler(IDbContext dbContext, IEmailHelper emailHelper, IOtpHelper otpHelper, ICacheService cacheService) : IRequestHandler<UpdateUserRequest, UpdateUserResponse>
{
    public async Task<UpdateUserResponse> Handle(UpdateUserRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct) ?? throw new NotFoundException();

        if (await dbContext.Users.AnyAsync(u => u.Id != request.Id && u.Email == request.Email, ct))
            throw new BadRequestException("Email is already taken.");

        var emailChanged = !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase);

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.AvatarUrl = request.AvatarUrl;

        if (emailChanged)
        {
            var (confirmationCode, expiresOn) = otpHelper.Generate();
            user.EmailConfirmed = false;
            user.OtpCode = confirmationCode;
            user.OtpCodeExpiresOn = expiresOn;
        }

        var rolesChanged = false;
        var normalizedRoles = request.Roles?.Normalize();
        if (normalizedRoles is not null && normalizedRoles.Length > 0)
        {
            var count = await dbContext.Roles.CountAsync(r => r.Name != null && normalizedRoles.Contains(r.Name), ct);
            if (count != normalizedRoles.Length)
                throw new BadRequestException("One or more specified roles do not exist.");

            var existingRoles = user.Roles
                .Where(role => !string.IsNullOrWhiteSpace(role.Name))
                .ToDictionary(
                    role => role.Name!,
                    role => role);
            var rolesToRemove = existingRoles
                .Where(role => normalizedRoles.Contains(role.Key) is false)
                .Select(role => role.Value)
                .ToArray();
            var rolesToAdd = normalizedRoles.Except(existingRoles.Keys).ToArray();

            if (rolesToRemove.Length > 0)
            {
                foreach (var role in rolesToRemove)
                    user.Roles.Remove(role);
            }

            if (rolesToAdd.Length > 0)
            {
                var roles = await dbContext.Roles
                    .Where(role => role.Name != null && rolesToAdd.Contains(role.Name))
                    .ToListAsync(ct);

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

        await cacheService.RemoveAsync(GetUserInfoHandler.CacheKey(user.Id), ct);

        return new UpdateUserResponse(user.Id);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleForEach(x => x.Roles).NotEmpty().MaximumLength(256);
    }
}
