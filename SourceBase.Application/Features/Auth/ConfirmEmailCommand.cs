using MediatR;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;

namespace SourceBase.Application.Features.Auth;

public record ConfirmEmailCommand(string Email, string Code) : IRequest;

public class ConfirmEmailCommandHandler(IIdentityContext identityContext) : IRequestHandler<ConfirmEmailCommand>
{
    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        await identityContext.ConfirmEmailAsync(request.Email, request.Code, Roles.User);
    }
}
