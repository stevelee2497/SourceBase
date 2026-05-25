using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Api.Features.Roles;

public record UpdateRoleRequest([property: SwaggerIgnore] Guid Id, string Name, string? Description);

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
        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new NotFoundException();
        role.Name = request.Name;
        role.NormalizedName = request.Name.ToUpper();
        role.Description = request.Description;

        await dbContext.SaveChangesAsync(ct);

        return new UpdateRoleResponse(role.Id);
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256)
            .NotEqual(AppRoles.Admin, StringComparer.OrdinalIgnoreCase)
            .WithMessage("Admin role cannot be updated")
            .MustAsync(async (request, name, ct) =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return true;

                return await dbContext.Roles.AnyAsync(role => role.Id != request.Id && role.NormalizedName == name.ToUpper(), ct) is false;
            })
            .WithMessage("Role name is already taken.");
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
