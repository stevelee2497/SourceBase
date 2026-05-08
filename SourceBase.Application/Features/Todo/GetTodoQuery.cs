using MediatR;
using SourceBase.Application.Abstractions;
using SourceBase.Application.Common;
using SourceBase.Domain.Entities;

namespace SourceBase.Application.Features.Todo;

public record GetTodoQuery(Guid Id) : IRequest<TodoItemDetailResponse>;

public class GetTodoQueryHandler(IDbContext dbContext) : IRequestHandler<GetTodoQuery, TodoItemDetailResponse>
{
    public async Task<TodoItemDetailResponse> Handle(GetTodoQuery request, CancellationToken cancellationToken)
    {
        var todo = await dbContext.TodoItems.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException();
        return new TodoItemDetailResponse(todo);
    }
}

public record TodoItemDetailResponse(
    Guid Id,
    DateOnly Date,
    string Title,
    ItemStatus Status,
    DateTime? CreatedOn,
    string? CreatedBy,
    DateTime? UpdatedOn,
    string? UpdatedBy)
{
    public TodoItemDetailResponse(TodoItemEntity todo)
        : this(todo.Id, todo.Date, todo.Title, todo.Status, todo.CreatedOn, todo.CreatedBy, todo.UpdatedOn, todo.UpdatedBy)
    {
    }
}
