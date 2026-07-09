using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

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

        var email = principal.FindFirstValue(ClaimTypes.Email);
        var emailVerified = principal.FindFirstValue(CustomClaimTypes.EmailVerified) == "true" || principal.HasClaim(c => c.Type == CustomClaimTypes.EmailVerified && c.Value == "True");

        var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId || (emailVerified && u.Email == email), ct);
        if (existingUser is null)
        {
            existingUser = await CreateGoogleUserAsync(googleId, email, ct);
        }

        if (existingUser.GoogleId != googleId)
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
        var baseUserName = email?.Split('@')[0].Replace(".", "").Replace("+", "").ToLower() ?? "user";

        var userRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == AppRoles.User, ct) ?? throw new ApiInternalException("Default user role not found. Please ensure the database is seeded correctly.");

        var user = new UserEntity
        {
            GoogleId = googleId,
            Email = email,
            UserName = $"{baseUserName}-{Guid.NewGuid().ToString("N")[..4]}",
            EmailConfirmed = true,
            PasswordHash = null,
            SecurityStamp = Guid.NewGuid().ToString(),
            Roles = [userRole]
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(ct);
        return user;
    }
}
