using MediatR;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;

namespace SourceBase.Application.Features.Auth;

public record RegisterCommand(string Email, string Password) : IRequest;

public class RegisterCommandHandler(IIdentityContext identityContext, IEmailHelper emailHelper, AppSettings appSettings) : IRequestHandler<RegisterCommand>
{
    public async Task Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        await identityContext.CreateUserAsync(request.Email, request.Password);
        var code = await identityContext.GenerateEmailConfirmationTokenAsync(request.Email);
        var confirmEmailUrl = $"{appSettings.WebUrl}/confirmEmail?email={request.Email}&code={code}";
        await emailHelper.SendEmailAsync(request.Email, "Confirm your email", $"Please confirm your account by clicking <a href='{confirmEmailUrl}'>here</a>.");
    }
}
