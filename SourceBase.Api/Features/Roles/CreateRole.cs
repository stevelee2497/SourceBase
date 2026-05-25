using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Roles;

public record CreateRoleRequest(string Name, string? Description);

public record CreateRoleResponse(Guid Id);

public class CreateRoleEndpoint : IEndpoint
{
    public const string Route = "roles";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateRoleRequest request, CreateRoleHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Roles");
}

public class CreateRoleHandler(RoleManager<RoleEntity> roleManager) : IRequestHandler<CreateRoleRequest, CreateRoleResponse>
{
    public async Task<CreateRoleResponse> Handle(CreateRoleRequest request, CancellationToken ct)
    {
        var role = new RoleEntity
        {
            Name = request.Name,
            Description = request.Description
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        return new CreateRoleResponse(role.Id);
    }
}

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
