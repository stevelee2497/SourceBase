using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Roles;

public record DeleteRoleCommand(Guid Id);

public record DeleteRoleResponse(bool Success);

public class DeleteRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete("/roles/{id:guid}", (Guid id, DeleteRoleHandler handler, CancellationToken ct) => handler.Handle(new DeleteRoleCommand(id), ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Roles");
}

public class DeleteRoleHandler(RoleManager<RoleEntity> roleManager) : IRequestHandler<DeleteRoleCommand, DeleteRoleResponse>
{
    public async Task<DeleteRoleResponse> Handle(DeleteRoleCommand request, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(request.Id.ToString()) ?? throw new NotFoundException();

        if (role.Name == AppRoles.Admin)
            throw new BadRequestException("Cannot delete Admin role");

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        return new DeleteRoleResponse(true);
    }
}
