using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

public class CreateRoleHandler(IDbContext dbContext) : IRequestHandler<CreateRoleRequest, CreateRoleResponse>
{
    public async Task<CreateRoleResponse> Handle(CreateRoleRequest request, CancellationToken ct)
    {
        var duplicateName = await dbContext.Roles.AnyAsync(r => r.NormalizedName == request.Name.ToUpper(), ct);
        if (duplicateName)
            throw new BadRequestException("Role name is already taken.");

        var role = new RoleEntity
        {
            Name = request.Name,
            NormalizedName = request.Name.ToUpper(),
            Description = request.Description,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(ct);

        return new CreateRoleResponse(role.Id);
    }
}

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
