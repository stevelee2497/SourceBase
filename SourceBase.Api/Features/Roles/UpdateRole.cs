using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace SourceBase.Api.Features.Roles;

public record UpdateRoleRequest([property: SwaggerIgnore] Guid Id, string Name, string? Description);

public record UpdateRoleResponse(Guid Id);

public class UpdateRoleEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPut("/roles/{id:guid}", (Guid id, [FromBody] UpdateRoleRequest body, UpdateRoleHandler handler, CancellationToken ct) => handler.Handle(body with { Id = id }, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Roles");
}

public class UpdateRoleHandler(RoleManager<RoleEntity> roleManager) : IRequestHandler<UpdateRoleRequest, UpdateRoleResponse>
{
    public async Task<UpdateRoleResponse> Handle(UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(request.Id.ToString()) ?? throw new NotFoundException();
        role.Name = request.Name;
        role.Description = request.Description;

        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        return new UpdateRoleResponse(role.Id);
    }
}

public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256).NotEqual(AppRoles.Admin, StringComparer.OrdinalIgnoreCase).WithMessage("Admin role cannot be updated");
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
