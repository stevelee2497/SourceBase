using FluentValidation;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class RefreshEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/refresh", ([FromBody] RefreshTokenRequest request, RefreshHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class RefreshHandler(SignInManager<UserEntity> signInManager, IOptionsMonitor<BearerTokenOptions> bearerTokenOptions) : IRequestHandler<RefreshTokenRequest, Results<Ok<LoginResponse>, EmptyHttpResult>>
{
    public async Task<Results<Ok<LoginResponse>, EmptyHttpResult>> Handle(RefreshTokenRequest request, CancellationToken ct)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(request.Token);
        var user = await signInManager.ValidateSecurityStampAsync(refreshTicket?.Principal);

        if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc || user == null)
            throw new UnAuthorizedException("Invalid token");

        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        await signInManager.SignInWithClaimsAsync(user, false, [new Claim("amr", "pwd")]);
        return TypedResults.Empty; // The actual token generation is handled by the JwtBearer middleware, so we return null here. The client will receive the token in the response headers.
    }
}

public record RefreshTokenRequest(string Token);

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
