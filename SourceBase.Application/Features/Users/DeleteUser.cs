using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Users;

public record DeleteUserRequest(Guid Id);

public record DeleteUserResponse(bool Success);

public class DeleteUserEndpoint : IEndpoint
{
    public const string Route = "users/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, ([AsParameters] DeleteUserRequest request, DeleteUserHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class DeleteUserHandler(IDbContext dbContext) : IRequestHandler<DeleteUserRequest, DeleteUserResponse>
{
    public async Task<DeleteUserResponse> Handle(DeleteUserRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        dbContext.Users.Remove(user!);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteUserResponse(true);
    }
}

public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .MustAsync(async (id, ct) => await dbContext.Users.AnyAsync(x => x.Id == id, ct))
            .WithMessage("User not found.");
    }
}
