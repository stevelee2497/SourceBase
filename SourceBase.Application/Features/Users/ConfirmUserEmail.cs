using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Users;

public record ConfirmUserEmailRequest(Guid Id);

public record ConfirmUserEmailResponse(bool Success);

public class ConfirmUserEmailEndpoint : IEndpoint
{
    public const string Route = "users/{id:guid}/confirm-email";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([AsParameters] ConfirmUserEmailRequest request, ConfirmUserEmailHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin })
        .WithTags("Users");
}

public class ConfirmUserEmailHandler(IDbContext dbContext) : IRequestHandler<ConfirmUserEmailRequest, ConfirmUserEmailResponse>
{
    public async Task<ConfirmUserEmailResponse> Handle(ConfirmUserEmailRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct);

        user!.EmailConfirmed = true;
        user.OtpCode = null;
        user.OtpCodeExpiresOn = null;

        await dbContext.SaveChangesAsync(ct);
        return new ConfirmUserEmailResponse(true);
    }
}

public class ConfirmUserEmailRequestValidator : AbstractValidator<ConfirmUserEmailRequest>
{
    public ConfirmUserEmailRequestValidator(IDbContext dbContext)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, ct) => await dbContext.Users.AnyAsync(u => u.Id == id, ct))
            .WithMessage("User not found.");
    }
}
