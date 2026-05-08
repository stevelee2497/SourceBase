using MediatR;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;

namespace SourceBase.Application.Features.Auth;

public record ResendConfirmationEmailCommand(string Email) : IRequest;

public class ResendConfirmationEmailCommandHandler(IIdentityService identityService, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<ResendConfirmationEmailCommand>
{
    public async Task Handle(ResendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        var code = await identityService.GenerateEmailConfirmationTokenAsync(request.Email);
        var confirmEmailUrl = $"{appSettings.WebUrl}/confirmEmail?email={request.Email}&code={code}";
        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Please confirm your account by clicking <a href='{confirmEmailUrl}'>here</a>.");
    }
}
