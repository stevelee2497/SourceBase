using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Users;

public record DeleteUserRequest(Guid Id);

public record DeleteUserResponse(bool Success);

public class DeleteUserEndpoint : IEndpoint
{
    public const string Route = "users/{id:guid}";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (Guid id, DeleteUserHandler handler, CancellationToken ct) => handler.Handle(new DeleteUserRequest(id), ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class DeleteUserHandler(IDbContext dbContext) : IRequestHandler<DeleteUserRequest, DeleteUserResponse>
{
    public async Task<DeleteUserResponse> Handle(DeleteUserRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new NotFoundException();
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(ct);

        return new DeleteUserResponse(true);
    }
}

public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
