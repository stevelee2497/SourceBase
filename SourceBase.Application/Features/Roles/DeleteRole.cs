using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Roles;

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
        dbContext.Roles.Remove(role!);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteRoleResponse(true);
    }
}

public class DeleteRoleRequestValidator : AbstractValidator<DeleteRoleRequest>
{
    public DeleteRoleRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .MustAsync(async (id, ct) => await dbContext.Roles.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Role not found.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Id)
                    .MustAsync(async (id, ct) =>
                    {
                        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id, ct);
                        return role?.Name != AppRoles.Admin;
                    })
                    .WithMessage("Cannot delete the Admin role.");
            });
    }
}