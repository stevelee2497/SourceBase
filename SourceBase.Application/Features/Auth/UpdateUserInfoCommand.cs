using MediatR;
using SourceBase.Application.Abstractions;

namespace SourceBase.Application.Features.Auth;

public record UpdateUserInfoCommand(string? FirstName, string? LastName, string? PhoneNumber, string[] Roles) : IRequest;

public class UpdateUserInfoCommandHandler(IIdentityService identityContext, IUserContext userContext) : IRequestHandler<UpdateUserInfoCommand>
{
    public async Task Handle(UpdateUserInfoCommand request, CancellationToken cancellationToken)
    {
        await identityContext.UpdateUserInfoAsync(userContext.UserId, request.FirstName, request.LastName, cancellationToken);
    }
}
