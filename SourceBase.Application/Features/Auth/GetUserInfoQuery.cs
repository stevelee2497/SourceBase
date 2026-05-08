using MediatR;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;

namespace SourceBase.Application.Features.Auth;

public record GetUserInfoQuery() : IRequest<UserInfoResponse>;

public class GetUserInfoQueryHandler(IIdentityContext identityContext, IUserContext userContext) : IRequestHandler<GetUserInfoQuery, UserInfoResponse>
{
    public async Task<UserInfoResponse> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
    {
        var user = await identityContext.GetUserWithRolesAsync(userContext.UserId, cancellationToken)
            ?? throw new UnAuthorizedException();

        return new UserInfoResponse(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, user.Roles.Select(r => r.Name!));
    }
}

public record UserInfoResponse(
    Guid Id,
    string? Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    IEnumerable<string> Roles);
