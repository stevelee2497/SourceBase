using MediatR;
using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.DbContexts;

namespace SourceBase.Api.Features.Todo;

public record GetTodoQuery(Guid Id) : IRequest<TodoItemDetailResponse>;

public class GetTodoQueryHandler(ApplicationDbContext dbContext) : IRequestHandler<GetTodoQuery, TodoItemDetailResponse>
{
    public async Task<TodoItemDetailResponse> Handle(GetTodoQuery request, CancellationToken cancellationToken)
    {
        var todo = await dbContext.TodoItems.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException();
        return new TodoItemDetailResponse(todo);
    }
}

public record TodoItemDetailResponse(Guid Id, DateOnly Date, string Title, ItemStatus Status, DateTime? CreatedOn, string? CreatedBy, DateTime? UpdatedOn, string? UpdatedBy)
{
    public TodoItemDetailResponse(TodoItemEntity todo)
        : this(todo.Id, todo.Date, todo.Title, todo.Status, todo.CreatedOn, todo.CreatedBy, todo.UpdatedOn, todo.UpdatedBy)
    {
    }
}

public static class GetTodoQueryEndpoint
{
    public static IEndpointRouteBuilder MapGetTodoQueryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/todos/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetTodoQuery(id), cancellationToken)))
            .WithTags("Todos");

        return endpoints;
    }
}
