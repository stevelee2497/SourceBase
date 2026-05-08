using MediatR;
using SourceBase.Application.Abstractions;

namespace SourceBase.Application.Features.Auth;

public record ResetPasswordCommand(string Email, string Code, string NewPassword) : IRequest;

public class ResetPasswordCommandHandler(IIdentityService identityContext) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        await identityContext.ResetPasswordAsync(request.Email, request.Code, request.NewPassword);
    }
}
