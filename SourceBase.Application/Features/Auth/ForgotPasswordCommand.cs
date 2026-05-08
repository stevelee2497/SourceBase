using MediatR;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;

namespace SourceBase.Application.Features.Auth;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler(IIdentityContext identityContext, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var code = await identityContext.GeneratePasswordResetTokenAsync(request.Email);
        var resetPasswordUrl = $"{appSettings.WebUrl}/resetPassword?email={request.Email}&code={code}";
        await emailHelper.SendEmailAsync(request.Email, "Reset Password", $"Click <a href='{resetPasswordUrl}'>here</a> to reset your password.");
    }
}
