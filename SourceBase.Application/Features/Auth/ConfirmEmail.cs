using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public record ConfirmEmailRequest(string Email, string Code);

public record ConfirmEmailResponse(bool Success);

public class ConfirmEmailEndpoint : IEndpoint
{
    public const string Route = "auth/confirmEmail";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] ConfirmEmailRequest request, ConfirmEmailHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ConfirmEmailHandler(IDbContext dbContext) : IRequestHandler<ConfirmEmailRequest, ConfirmEmailResponse>
{
    public async Task<ConfirmEmailResponse> Handle(ConfirmEmailRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email, ct) ?? throw new UnAuthorizedException("Invalid credentials");

        if (user.OtpCode != request.Code || user.OtpCodeExpiresOn is null || user.OtpCodeExpiresOn <= DateTime.UtcNow)
            throw new UnAuthorizedException("Invalid or expired code");

        user.EmailConfirmed = true;

        var userRole = await dbContext.Roles.FirstOrDefaultAsync(x => x.Name == AppRoles.User, ct) ?? throw new ApiInternalException("Default user role not found");
        user.Roles.Add(userRole!);

        await dbContext.SaveChangesAsync(ct);

        return new ConfirmEmailResponse(true);
    }
}

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
    }
}
