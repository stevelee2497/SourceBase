using FluentValidation;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/login", ([FromBody] LoginRequest request, IRequestHandler<LoginRequest, Results<Ok<LoginResponse>, EmptyHttpResult>> handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class LoginHandler(UserManager<UserEntity> userManager, SignInManager<UserEntity> signInManager) : IRequestHandler<LoginRequest, Results<Ok<LoginResponse>, EmptyHttpResult>>
{
    public async Task<Results<Ok<LoginResponse>, EmptyHttpResult>> Handle(LoginRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || !await userManager.IsEmailConfirmedAsync(user))
            throw new UnAuthorizedException("Invalid credentials");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
            throw new UnAuthorizedException("Invalid credentials");

        signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        await signInManager.SignInWithClaimsAsync(user, false, [new Claim("amr", "pwd")]);
        return TypedResults.Empty; // The actual token generation is handled by the JwtBearer middleware, so we return null here. The client will receive the token in the response headers.
    }
}

public record LoginRequest(string Email, string Password);

public record LoginResponse(string TokenType, string AccessToken, int expiresIn, string RefreshToken);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}