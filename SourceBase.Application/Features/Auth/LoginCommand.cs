using MediatR;
using SourceBase.Application.Abstractions;

namespace SourceBase.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest;

public class LoginCommandHandler(IIdentityService identityContext) : IRequestHandler<LoginCommand>
{
    public async Task Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        await identityContext.ValidateAndSignInAsync(request.Email, request.Password);
    }
}
