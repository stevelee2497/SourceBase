using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class Logout : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPost("/auth/logout",
        (ISender sender, CancellationToken ct) => sender.Send(new LogoutCommand(), ct)).WithTags("Auth");
}

public class LogoutHandler(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager) : IRequestHandler<LogoutCommand, Results<Ok, EmptyHttpResult>>
{
    public async Task<Results<Ok, EmptyHttpResult>> Handle(LogoutCommand request, CancellationToken ct)
    {
        await signInManager.SignOutAsync();
        var user = await userManager.GetUserAsync(signInManager.Context.User);
        await userManager.UpdateSecurityStampAsync(user!);
        return TypedResults.Ok();
    }
}

public record LogoutCommand : IRequest<Results<Ok, EmptyHttpResult>>;