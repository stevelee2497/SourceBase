using Microsoft.EntityFrameworkCore;
using SourceBase.Api.Entities;
using SourceBase.Api.Shared.Interfaces;

namespace SourceBase.Api.Features.Data;

public record GetStatsRequest;

public record GetStatsResponse(int UserCount, int TotalTodoLists, int TotalTodoItems, int CompletedTodoItems, int TotalWallets, int TotalTransactions);

public class GetStatsEndpoint : IEndpoint
{
    public const string Route = "data/stats";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapGet(Route, (GetStatsHandler handler, CancellationToken ct) => handler.Handle(new GetStatsRequest(), ct))
        .WithTags("Data");
}

public class GetStatsHandler(IDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<GetStatsRequest, GetStatsResponse>
{
    public async Task<GetStatsResponse> Handle(GetStatsRequest request, CancellationToken ct)
    {
        var userCount = await dbContext.Users.CountAsync(ct);
        var totalTodoLists = await dbContext.TodoLists.CountAsync(ct);
        var totalTodoItems = await dbContext.TodoItems.CountAsync(ct);
        var completedTodoItems = await dbContext.TodoItems.CountAsync(x => x.Status == TodoItemStatus.Completed, ct);
        var totalWallets = await dbContext.Wallets.CountAsync(x => x.UserId == currentUser.UserId, ct);
        var totalTransactions = await dbContext.Transactions.CountAsync(x => x.UserId == currentUser.UserId, ct);

        return new GetStatsResponse(userCount, totalTodoLists, totalTodoItems, completedTodoItems, totalWallets, totalTransactions);
    }
}
