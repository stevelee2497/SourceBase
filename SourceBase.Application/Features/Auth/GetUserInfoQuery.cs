using MediatR;
using SourceBase.Application.Abstractions;

namespace SourceBase.Application.Features.Auth;

public record GetUserInfoQuery() : IRequest<UserInfoResponse>;

public class GetUserInfoQueryHandler(IIdentityService identityService, IUserContext userContext) : IRequestHandler<GetUserInfoQuery, UserInfoResponse>
{
    public Task<UserInfoResponse> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
    {
        return identityService.GetUserInfoAsync(userContext.UserId, cancellationToken);
    }
}

public record UserInfoResponse(Guid Id, string? Email, string? FirstName, string? LastName, string? PhoneNumber, IEnumerable<string> Roles);
