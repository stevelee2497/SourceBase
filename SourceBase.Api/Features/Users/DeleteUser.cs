using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Users;

public record DeleteUserCommand(Guid Id);

public record DeleteUserResponse(bool Success);

public class DeleteUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete("/users/{id:guid}", (Guid id, DeleteUserHandler handler, CancellationToken ct) => handler.Handle(new DeleteUserCommand(id), ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class DeleteUserHandler(UserManager<UserEntity> userManager) : IRequestHandler<DeleteUserCommand, DeleteUserResponse>
{
    public async Task<DeleteUserResponse> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(request.Id.ToString()) ?? throw new NotFoundException();
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        return new DeleteUserResponse(true);
    }
}

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
