using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Application.Features.Roles;

public record UpdateRoleRequest([property: SwaggerIgnore] Guid Id, string Name, string Description);

public record UpdateRoleResponse(Guid Id);

public class UpdateRoleEndpoint : IEndpoint
{
    public const string Route = "roles/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut(Route, (Guid id, [FromBody] UpdateRoleRequest body, UpdateRoleHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Roles");
}

public class UpdateRoleHandler(IDbContext dbContext) : IRequestHandler<UpdateRoleRequest, UpdateRoleResponse>
{
    public async Task<UpdateRoleResponse> Handle(UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await dbContext.Roles.FindAsync([request.Id], ct) ?? throw new NotFoundException();

        if (role.Name == AppRoles.Admin)
            throw new BadRequestException("Admin role cannot be updated.");

        var duplicateName = await dbContext.Roles.AnyAsync(r => r.Id != request.Id && r.Name == request.Name, ct);
        if (duplicateName)
            throw new BadRequestException("Role name is already taken.");

        role.Name = request.Name;
        role.Description = request.Description;

        await dbContext.SaveChangesAsync(ct);

        return new UpdateRoleResponse(role.Id);
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}
