using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public record RefreshTokenRequest(string Token);

public class RefreshEndpoint : IEndpoint
{
    public const string Route = "auth/refresh";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] RefreshTokenRequest request, RefreshHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class RefreshHandler(IDbContext dbContext, ISecurityProvider securityProvider, IHttpContextAccessor httpContextAccessor) : IRequestHandler<RefreshTokenRequest, Results<Ok<LoginResponse>, EmptyHttpResult>>
{
    public async Task<Results<Ok<LoginResponse>, EmptyHttpResult>> Handle(RefreshTokenRequest request, CancellationToken ct)
    {
        var refreshPrincipal = securityProvider.ParseRefreshToken(request.Token);
        var userId = refreshPrincipal.UserId;
        var user = await dbContext.Users.Include(x => x.Roles).FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw new UnAuthorizedException("User not found");
        if (user.SecurityStamp != refreshPrincipal.SecurityStamp)
            throw new UnAuthorizedException("Invalid token");

        var claimsPrincipal = securityProvider.CreateClaimsPrincipal(user);
        await httpContextAccessor.HttpContext!.SignInAsync(Constants.BearerScheme, claimsPrincipal);
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
