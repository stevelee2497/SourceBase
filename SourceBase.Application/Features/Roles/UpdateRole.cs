using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Roles;

public record UpdateRoleRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, string? Name, string? Description);

public record UpdateRoleResponse(Guid Id);

public class UpdateRoleEndpoint : IEndpoint
{
    public const string Route = "roles/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPatch(Route, ([FromBody] UpdateRoleRequest body, UpdateRoleHandler handler, CancellationToken ct) => handler.Handle(body, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Roles");
}

public class UpdateRoleHandler(IDbContext dbContext) : IRequestHandler<UpdateRoleRequest, UpdateRoleResponse>
{
    public async Task<UpdateRoleResponse> Handle(UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await dbContext.Roles.FindAsync([request.Id], ct)!;

        role!.Name = request.Name ?? role.Name;
        role.Description = request.Description ?? role.Description;

        await dbContext.SaveChangesAsync(ct);

        return new UpdateRoleResponse(role.Id);
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .MustAsync((id, ct) => dbContext.Roles.AnyAsync(r => r.Id == id, ct))
            .WithMessage("Role not found.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Id)
                    .MustAsync(async (id, ct) =>
                    {
                        var role = await dbContext.Roles.FindAsync([id], ct);
                        return role?.Name != AppRoles.Admin;
                    })
                    .WithMessage("Admin role cannot be updated.");
            });

        RuleFor(x => x.Name).NotEmpty().MaximumLength(256).When(x => x.Name is not null);
        RuleFor(x => x.Name)
            .MustAsync((req, name, ct) => dbContext.Roles.AllAsync(r => r.Id == req.Id || r.Name != name, ct))
            .WithMessage("Role name is already taken.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description).NotEmpty().MaximumLength(500).When(x => x.Description is not null);
    }
}
