using MediatR;
using SourceBase.Application.Abstractions;

namespace SourceBase.Application.Features.Auth;

public record RefreshTokenCommand(string Token) : IRequest;

public class RefreshTokenCommandHandler(IIdentityService identityService) : IRequestHandler<RefreshTokenCommand>
{
    public async Task Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        await identityService.RefreshTokenAsync(request.Token);
    }
}
