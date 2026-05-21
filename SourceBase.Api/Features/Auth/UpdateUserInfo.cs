using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class UpdateUserInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut("/auth/info", ([FromBody] UpdateUserInfoRequest request, UpdateUserInfoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Auth");
}

public class UpdateUserInfoHandler(
    UserManager<UserEntity> userManager,
    RoleManager<RoleEntity> roleManager,
    ICurrentUser currentUser) : IRequestHandler<UpdateUserInfoRequest, NoContent>
{
    public async Task<NoContent> Handle(UpdateUserInfoRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString()) ?? throw new NotFoundException();
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        if (request.Roles is not null)
        {
            var normalizedRoles = request.Roles
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var role in normalizedRoles)
            {
                if (await roleManager.RoleExistsAsync(role) is false)
                    throw new BadRequestException($"Role '{role}' does not exist");
            }

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
        }

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        return TypedResults.NoContent();
    }
}

public record UpdateUserInfoRequest(string? FirstName, string? LastName, string? PhoneNumber, string[]? Roles);

public class UpdateUserInfoRequestValidator : AbstractValidator<UpdateUserInfoRequest>
{
    public UpdateUserInfoRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100).When(x => x.FirstName is not null);
        RuleFor(x => x.LastName).MaximumLength(100).When(x => x.LastName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        RuleForEach(x => x.Roles).NotEmpty().MaximumLength(256);
    }
}
