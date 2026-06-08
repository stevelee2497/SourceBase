using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;
using SourceBase.Domain;

namespace SourceBase.Application.Features.Users;

public record ConfirmUserEmailRequest(Guid Id);

public record ConfirmUserEmailResponse(bool Success);

public class ConfirmUserEmailEndpoint : IEndpoint
{
    public const string Route = "users/{id:guid}/confirm-email";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, (Guid id, ConfirmUserEmailHandler handler, CancellationToken ct) => handler.Handle(new ConfirmUserEmailRequest(id), ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class ConfirmUserEmailHandler(IDbContext dbContext) : IRequestHandler<ConfirmUserEmailRequest, ConfirmUserEmailResponse>
{
    public async Task<ConfirmUserEmailResponse> Handle(ConfirmUserEmailRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException();

        user.EmailConfirmed = true;
        user.OtpCode = null;
        user.OtpCodeExpiresOn = null;

        await dbContext.SaveChangesAsync(ct);
        return new ConfirmUserEmailResponse(true);
    }
}
