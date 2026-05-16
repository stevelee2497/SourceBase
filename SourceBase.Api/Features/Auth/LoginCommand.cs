using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Utilities;

namespace SourceBase.Api.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest;

public class LoginCommandHandler(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : IRequestHandler<LoginCommand>
{
    public async Task Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || !await userManager.IsEmailConfirmedAsync(user))
        {
            throw new UnAuthorizedException("Invalid credentials");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnAuthorizedException("Invalid credentials");
        }

        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        await signInManager.SignInWithClaimsAsync(user, false, [new Claim("amr", "pwd")]);
    }
}

public class LoginCommandEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/auth/login", async (LoginCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
            })
            .WithTags("Auth")
            .AllowAnonymous();
}
