using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Roles;

public record DeleteRoleRequest(Guid Id);

public record DeleteRoleResponse(bool Success);

public class DeleteRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete("/roles/{id:guid}", (Guid id, DeleteRoleHandler handler, CancellationToken ct) => handler.Handle(new DeleteRoleRequest(id), ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Roles");
}

public class DeleteRoleHandler(RoleManager<RoleEntity> roleManager) : IRequestHandler<DeleteRoleRequest, DeleteRoleResponse>
{
    public async Task<DeleteRoleResponse> Handle(DeleteRoleRequest request, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(request.Id.ToString());

        var result = await roleManager.DeleteAsync(role!);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        return new DeleteRoleResponse(true);
    }
}

public class DeleteRoleRequestValidator : AbstractValidator<DeleteRoleRequest>
{
    public DeleteRoleRequestValidator(RoleManager<RoleEntity> roleManager)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .MustAsync(async (id, ct) =>
            {
                var role = await roleManager.FindByIdAsync(id.ToString());
                return role != null && role.Name != AppRoles.Admin;
            })
            .WithMessage("Role with the specified ID does not exist or cannot delete Admin role");
    }
}