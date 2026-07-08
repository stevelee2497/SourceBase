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
