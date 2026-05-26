using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Roles;

public record DeleteRoleRequest(Guid Id);

public record DeleteRoleResponse(bool Success);

public class DeleteRoleEndpoint : IEndpoint
{
    public const string Route = "roles/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteRoleRequest request, DeleteRoleHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Roles");
}

public class DeleteRoleHandler(IDbContext dbContext) : IRequestHandler<DeleteRoleRequest, DeleteRoleResponse>
{
    public async Task<DeleteRoleResponse> Handle(DeleteRoleRequest request, CancellationToken ct)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (role == null || role.Name == AppRoles.Admin)
            throw new BadRequestException("Role not found or cannot delete Admin role.");

        dbContext.Roles.Remove(role);
        await dbContext.SaveChangesAsync(ct);

        return new DeleteRoleResponse(true);
    }
}

public class DeleteRoleRequestValidator : AbstractValidator<DeleteRoleRequest>
{
    public DeleteRoleRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}