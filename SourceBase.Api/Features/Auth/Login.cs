using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string TokenType, string AccessToken, int ExpiresIn, string RefreshToken);

public class LoginEndpoint : IEndpoint
{
    public const string Route = "auth/login";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] LoginRequest request, LoginHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class LoginHandler(IDbContext dbContext, ISecurityProvider securityProvider, IHttpContextAccessor httpContextAccessor) : IRequestHandler<LoginRequest, Results<Ok<LoginResponse>, EmptyHttpResult>>
{
    public async Task<Results<Ok<LoginResponse>, EmptyHttpResult>> Handle(LoginRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Email == request.Email, ct);

        if (user == null || !user.EmailConfirmed || !securityProvider.VerifyPassword(user, request.Password))
            throw new UnAuthorizedException("Invalid credentials");

        var claimsPrincipal = securityProvider.CreateClaimsPrincipal(user);
        await httpContextAccessor.HttpContext!.SignInAsync(Constants.BearerScheme, claimsPrincipal);
        return TypedResults.Empty; // The actual token generation is handled by the JwtBearer middleware, so we return null here. The client will receive the token in the response headers.
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}