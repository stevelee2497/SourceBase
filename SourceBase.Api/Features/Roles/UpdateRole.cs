using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Roles;

public class UpdateRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut("/roles/{id:guid}", (Guid id, [FromBody] UpdateRoleRequest request, UpdateRoleHandler handler, CancellationToken ct) => handler.Handle(new UpdateRoleCommand(id, request.Name, request.Description), ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Roles");
}

public class UpdateRoleHandler(RoleManager<RoleEntity> roleManager) : IRequestHandler<UpdateRoleCommand, NoContent>
{
    public async Task<NoContent> Handle(UpdateRoleCommand request, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(request.Id.ToString()) ?? throw new NotFoundException();
        role.Name = request.Name.Trim();
        role.Description = request.Description?.Trim();

        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        return TypedResults.NoContent();
    }
}

public record UpdateRoleCommand(Guid Id, string Name, string? Description);

public record UpdateRoleRequest(string Name, string? Description);

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256).NotEqual(AppRoles.Admin, StringComparer.OrdinalIgnoreCase).WithMessage("Admin role cannot be updated");
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
