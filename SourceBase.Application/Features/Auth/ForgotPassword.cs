using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SourceBase.Application.Shared;
using SourceBase.Application.Shared.Interfaces;

namespace SourceBase.Application.Features.Auth;

public record ForgotPasswordRequest(string Email);

public record ForgotPasswordResponse(bool Success);

public class ForgotPasswordEndpoint : IEndpoint
{
    public const string Route = "auth/forgotPassword";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] ForgotPasswordRequest request, ForgotPasswordHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .AllowAnonymous()
        .WithTags("Auth");
}

public class ForgotPasswordHandler(IDbContext dbContext, IEmailHelper emailHelper, IOtpHelper otpHelper) : IRequestHandler<ForgotPasswordRequest, ForgotPasswordResponse>
{
    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordRequest request, CancellationToken ct)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email, ct) ?? throw new NotFoundException("User not found");
        var (otp, expiresOn) = otpHelper.Generate();
        user.OtpCode = otp;
        user.OtpCodeExpiresOn = expiresOn;
        await dbContext.SaveChangesAsync(ct);
        await emailHelper.SendEmailAsync(request.Email, "Reset Password", $"Your password reset code is: <b>{otp}</b>");
        return new ForgotPasswordResponse(true);
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
