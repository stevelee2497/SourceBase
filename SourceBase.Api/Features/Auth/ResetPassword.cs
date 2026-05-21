using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Auth;

public class ResetPasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/auth/resetPassword", ([FromBody] ResetPasswordRequest request, ResetPasswordHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ResetPasswordHandler(UserManager<UserEntity> userManager) : IRequestHandler<ResetPasswordRequest, NoContent>
{
    public async Task<NoContent> Handle(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email) ?? throw new NotFoundException("User not found");
        if (user.OtpCode != request.Code)
            throw new BadRequestException("Invalid or expired code");

        user.OtpCode = null;
        await userManager.UpdateAsync(user);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        return TypedResults.NoContent();
    }
}

public record ResetPasswordRequest(string Email, string Code, string NewPassword);

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Length(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}
