using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class Logout : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app.MapPost("/auth/logout", Handler).WithTags("Auth");

    private async Task<Results<Ok, EmptyHttpResult>> Handler(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager, CancellationToken ct)
    {
        await signInManager.SignOutAsync();
        var user = await userManager.GetUserAsync(signInManager.Context.User);
        await userManager.UpdateSecurityStampAsync(user!);
        return TypedResults.Ok();
    }
}