using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record LogoutRequest;

public record LogoutResponse(bool Success);

public class LogoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/logout", (LogoutHandler handler, CancellationToken ct) => handler.Handle(new LogoutRequest(), ct))
        .WithTags("Auth");
}

public class LogoutHandler(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager) : IRequestHandler<LogoutRequest, LogoutResponse>
{
    public async Task<LogoutResponse> Handle(LogoutRequest request, CancellationToken ct)
    {
        await signInManager.SignOutAsync();
        var user = await userManager.GetUserAsync(signInManager.Context.User);
        await userManager.UpdateSecurityStampAsync(user!);
        return new LogoutResponse(true);
    }
}