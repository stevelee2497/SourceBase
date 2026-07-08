using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

// ── Initiate Login with Google ────────────────────────────────────────────────

public class GoogleLoginEndpoint : IEndpoint
{
    public const string Route = "auth/google";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (HttpContext ctx) =>
        {
            var props = new AuthenticationProperties { RedirectUri = "/api/auth/google/complete" };
            return ctx.ChallengeAsync("Google", props);
        })
        .AllowAnonymous()
        .WithTags("Auth");
}

// ── Initiate Connect (Authenticated) ─────────────────────────────────────────

public record PrepareGoogleConnectRequest;
public record PrepareGoogleConnectResponse(string State);

public class PrepareGoogleConnectEndpoint : IEndpoint
{
    public const string Route = "auth/google/connect/prepare";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (PrepareGoogleConnectHandler handler, CancellationToken ct) => handler.Handle(new PrepareGoogleConnectRequest(), ct))
        .WithTags("Auth");
}

public class PrepareGoogleConnectHandler(ICacheService cacheService, ICurrentUser currentUser) : IRequestHandler<PrepareGoogleConnectRequest, PrepareGoogleConnectResponse>
{
    public async Task<PrepareGoogleConnectResponse> Handle(PrepareGoogleConnectRequest request, CancellationToken ct)
    {
        var state = Guid.NewGuid().ToString("N");
        await cacheService.SetAsync(CacheKeys.GoogleConnectState.WithState(state), currentUser.UserId.ToString()!, TimeSpan.FromMinutes(5), ct);
        return new PrepareGoogleConnectResponse(state);
    }
}

// ── Initiate Connect OAuth (browser redirect) ─────────────────────────────────

public class GoogleConnectEndpoint : IEndpoint
{
    public const string Route = "auth/google/connect";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, async (string state, HttpContext ctx, ICacheService cacheService, CancellationToken ct) =>
        {
            var stored = await cacheService.GetAsync<string>(CacheKeys.GoogleConnectState.WithState(state), ct);
            if (stored is null) return Results.BadRequest("Invalid or expired state token.");

            var props = new AuthenticationProperties { RedirectUri = "/api/auth/google/complete" };
            props.Items["connect_state"] = state;
            await ctx.ChallengeAsync("Google", props);
            return Results.Empty;
        })
        .AllowAnonymous()
        .WithTags("Auth");
}

// ── OAuth Callback: complete login or connect ─────────────────────────────────

public record GoogleCompleteRequest;

public class GoogleCompleteEndpoint : IEndpoint
{
    public const string Route = "auth/google/complete";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (GoogleCompleteHandler handler, CancellationToken ct) => handler.Handle(new GoogleCompleteRequest(), ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class GoogleCompleteHandler(IDbContext dbContext, ICacheService cacheService, IHttpContextAccessor httpContextAccessor, AppSettings appSettings) : IRequestHandler<GoogleCompleteRequest, EmptyHttpResult>
{
    public async Task<EmptyHttpResult> Handle(GoogleCompleteRequest request, CancellationToken ct)
    {
        var ctx = httpContextAccessor.HttpContext!;
        var result = await ctx.AuthenticateAsync(Constants.ExternalScheme);
        if (!result.Succeeded)
        {
            ctx.Response.Redirect($"{appSettings.GoogleOAuth.FrontendUrl}/login?google_error=oauth_failed");
            return TypedResults.Empty;
        }

        var principal = result.Principal;
        var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var isEmailVerified = principal.FindFirstValue("email_verified") == "true"
            || principal.HasClaim(c => c.Type == "email_verified" && c.Value == "True");

        await ctx.SignOutAsync(Constants.ExternalScheme);

        // ── Connect mode ──────────────────────────────────────────────────────
        if (result.Properties?.Items.TryGetValue("connect_state", out var connectState) == true && connectState is not null)
        {
            var userIdStr = await cacheService.GetAsync<string>(CacheKeys.GoogleConnectState.WithState(connectState), ct);
            if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            {
                ctx.Response.Redirect($"{appSettings.GoogleOAuth.FrontendUrl}/login?google_error=oauth_failed");
                return TypedResults.Empty;
            }

            await cacheService.RemoveAsync(CacheKeys.GoogleConnectState.WithState(connectState), ct);

            if (await dbContext.Users.AnyAsync(u => u.GoogleId == googleId && u.Id != userId, ct))
            {
                ctx.Response.Redirect($"{appSettings.GoogleOAuth.FrontendUrl}/login?google_error=already_linked");
                return TypedResults.Empty;
            }

            var user = await dbContext.Users.FindAsync([userId], ct);
            if (user is not null)
            {
                user.GoogleId = googleId;
                await dbContext.SaveChangesAsync(ct);
                await cacheService.RemoveAsync(CacheKeys.UserInfo.WithId(userId), ct);
            }

            ctx.Response.Redirect($"{appSettings.GoogleOAuth.FrontendUrl}/?google_connected=true");
            return TypedResults.Empty;
        }

        // ── Login mode ────────────────────────────────────────────────────────
        var existingUser = await dbContext.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.GoogleId == googleId, ct);

        if (existingUser is null && isEmailVerified && email is not null)
            existingUser = await dbContext.Users.Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (existingUser is null)
        {
            existingUser = await CreateGoogleUserAsync(googleId, email, ct);
        }
        else if (existingUser.GoogleId != googleId)
        {
            existingUser.GoogleId = googleId;
            await dbContext.SaveChangesAsync(ct);
        }

        var code = Guid.NewGuid().ToString("N");
        await cacheService.SetAsync(CacheKeys.GoogleExchange.WithCode(code), existingUser.Id.ToString(), TimeSpan.FromMinutes(2), ct);

        ctx.Response.Redirect($"{appSettings.GoogleOAuth.FrontendUrl}/auth/google/callback?code={code}");
        return TypedResults.Empty;
    }

    private async Task<UserEntity> CreateGoogleUserAsync(string googleId, string? email, CancellationToken ct)
    {
        var baseUserName = email is not null
            ? email.Split('@')[0].Replace(".", "").Replace("+", "").ToLower()
            : "user";

        var userName = baseUserName;
        if (await dbContext.Users.AnyAsync(u => u.UserName == userName, ct))
            userName = $"{baseUserName}-{Guid.NewGuid().ToString("N")[..4]}";

        var userRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.User, ct);

        var user = new UserEntity
        {
            GoogleId = googleId,
            Email = email,
            UserName = userName,
            EmailConfirmed = true,
            PasswordHash = null,
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        if (userRole is not null) user.Roles.Add(userRole);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(ct);
        return user;
    }
}

// ── Exchange code for bearer tokens ──────────────────────────────────────────

public class GoogleExchangeEndpoint : IEndpoint
{
    public const string Route = "auth/google/exchange";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (string code, GoogleExchangeHandler handler, CancellationToken ct) => handler.Handle(code, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class GoogleExchangeHandler(IDbContext dbContext, ICacheService cacheService, ISecurityProvider securityProvider, IHttpContextAccessor httpContextAccessor) : IRequestHandler<string, EmptyHttpResult>
{
    public async Task<EmptyHttpResult> Handle(string code, CancellationToken ct)
    {
        var userIdStr = await cacheService.GetAsync<string>(CacheKeys.GoogleExchange.WithCode(code), ct);
        if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            throw new UnAuthorizedException("Invalid or expired exchange code.");

        await cacheService.RemoveAsync(CacheKeys.GoogleExchange.WithCode(code), ct);

        var user = await dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnAuthorizedException("User not found.");

        var claimsPrincipal = securityProvider.CreateClaimsPrincipal(user);
        await httpContextAccessor.HttpContext!.SignInAsync(Constants.BearerScheme, claimsPrincipal);
        return TypedResults.Empty;
    }
}

// ── Disconnect Google ─────────────────────────────────────────────────────────

public record DisconnectGoogleRequest;
public record DisconnectGoogleResponse(bool Success);

public class DisconnectGoogleEndpoint : IEndpoint
{
    public const string Route = "auth/google/disconnect";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapDelete(Route, (DisconnectGoogleHandler handler, CancellationToken ct) => handler.Handle(new DisconnectGoogleRequest(), ct))
        .WithTags("Auth");
}

public class DisconnectGoogleHandler(IDbContext dbContext, ICurrentUser currentUser, ICacheService cacheService) : IRequestHandler<DisconnectGoogleRequest, DisconnectGoogleResponse>
{
    public async Task<DisconnectGoogleResponse> Handle(DisconnectGoogleRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        if (user.PasswordHash is null)
            throw new BadRequestException("Cannot disconnect Google when no password is set.");

        user.GoogleId = null;
        await dbContext.SaveChangesAsync(ct);
        await cacheService.RemoveAsync(CacheKeys.UserInfo.WithId(currentUser.UserId), ct);

        return new DisconnectGoogleResponse(true);
    }
}
