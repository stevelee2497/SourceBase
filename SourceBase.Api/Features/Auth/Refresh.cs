using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record RefreshTokenRequest(string Token);

public class RefreshEndpoint : IEndpoint
{
    public const string Route = "auth/refresh";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] RefreshTokenRequest request, RefreshHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class RefreshHandler(IDbContext dbContext, IHttpContextAccessor httpContextAccessor, IOptionsMonitor<BearerTokenOptions> bearerTokenOptions, IClaimsManager claimsManager) : IRequestHandler<RefreshTokenRequest, Results<Ok<LoginResponse>, EmptyHttpResult>>
{
    public async Task<Results<Ok<LoginResponse>, EmptyHttpResult>> Handle(RefreshTokenRequest request, CancellationToken ct)
    {
        var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
        var refreshTicket = refreshTokenProtector.Unprotect(request.Token);
        if (refreshTicket?.Properties.ExpiresUtc is not { } expiresUtc || DateTimeOffset.UtcNow >= expiresUtc)
            throw new UnAuthorizedException("Invalid token");

        var userId = refreshTicket?.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnAuthorizedException("Invalid token");
        var user = await dbContext.Users.Include(x => x.Roles).FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId), ct) ?? throw new UnAuthorizedException("User not found");
        if (user.SecurityStamp != refreshTicket.Principal.FindFirst(Constants.SecurityStampClaimType)?.Value)
            throw new UnAuthorizedException("Invalid token");

        var claims = await claimsManager.CreateClaimsPrincipalAsync(user);
        await httpContextAccessor.HttpContext!.SignInAsync(IdentityConstants.BearerScheme, claims);
        return TypedResults.Empty; // The actual token generation is handled by the JwtBearer middleware, so we return null here. The client will receive the token in the response headers.
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
