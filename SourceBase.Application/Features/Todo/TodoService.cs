using Microsoft.EntityFrameworkCore;
using SourceBase.Domain.Abstractions;
using SourceBase.Domain.Common;
using SourceBase.Domain.Entities;

namespace SourceBase.Application.Features.Todo;

[ScopedDependency<ITodoService>]
public class TodoService(IDbContext dbContext, IUserContext userContext) : ITodoService
{
    public async Task<TodoItemDetailResponse> GetTodoAsync(Guid id)
    {
        var todo = await dbContext.TodoItems.FindAsync(id) ?? throw new NotFoundException();
        return new TodoItemDetailResponse(todo);
    }

    public async Task<IEnumerable<TodoItemDetailResponse>> GetTodosAsync()
    {
        return await dbContext.TodoItems
            .Where(x => x.UserId == userContext.CurrentUserId)
            .Select(todo => new TodoItemDetailResponse(todo))
            .ToListAsync();
    }

    public async Task CreateTodoAsync(CreateTodoRequest todoItem)
    {
        dbContext.TodoItems.Add(new TodoItemEntity { Title = todoItem.Title, Date = todoItem.Date, UserId = userContext.CurrentUserId });
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateTodoAsync(Guid id, CreateTodoRequest todoItem)
    {
        var item = await dbContext.TodoItems.FindAsync(id) ?? throw new NotFoundException();
        item.Title = todoItem.Title;
        item.Status = todoItem.Status;
        item.Date = todoItem.Date;
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteTodoAsync(Guid id)
    {
        var item = await dbContext.TodoItems.FindAsync(id) ?? throw new NotFoundException();
        dbContext.TodoItems.Remove(item);
        await dbContext.SaveChangesAsync();
    }
}

