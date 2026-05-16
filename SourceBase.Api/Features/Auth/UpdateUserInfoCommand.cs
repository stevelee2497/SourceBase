using MediatR;
using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.Interfaces;
using SourceBase.Api.Utilities;

namespace SourceBase.Api.Features.Auth;

public record UpdateUserInfoCommand(string? FirstName, string? LastName, string? PhoneNumber, string[] Roles) : IRequest;

public class UpdateUserInfoCommandHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<UpdateUserInfoCommand>
{
    public async Task Handle(UpdateUserInfoCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken) ?? throw new NotFoundException();
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class UpdateUserInfoCommandEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPut("/auth/info", async (UpdateUserInfoCommand command, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(command, cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Auth");
}
