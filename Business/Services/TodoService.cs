using Core.Contexts;
using Core.Entities;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public interface ITodoService
{
    Task<IEnumerable<TodoItemDetailDto>> GetTodoItemsAsync();
    Task<TodoItemDetailDto> GetTodoAsync(Guid id);
    Task CreateTodoAsync(TodoItemDto todoItem);
    Task UpdateTodoAsync(Guid id, TodoItemDto todoItem);
    Task DeleteTodoAsync(Guid id);
}

public class TodoService(IDbContext dbContext, IUserContext userContext) : ITodoService
{
    public async Task<TodoItemDetailDto> GetTodoAsync(Guid id)
    {
        var todo = await dbContext.TodoItems.FindAsync(id) ?? throw new NotFoundException();
        return new TodoItemDetailDto(todo.Id, todo.Date, todo.Title, todo.Status, todo.CreatedOn);
    }

    public async Task<IEnumerable<TodoItemDetailDto>> GetTodoItemsAsync()
    {
        return await dbContext.TodoItems
            .Where(x => x.UserId == userContext.CurrentUserId)
            .Select(todo => new TodoItemDetailDto(todo.Id, todo.Date, todo.Title, todo.Status, todo.CreatedOn))
            .ToListAsync();
    }

    public async Task CreateTodoAsync(TodoItemDto todoItem)
    {
        dbContext.TodoItems.Add(new TodoItemEntity { Title = todoItem.Title, Date = todoItem.Date, UserId = userContext.CurrentUserId });
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateTodoAsync(Guid id, TodoItemDto todoItem)
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

public record TodoItemDto(DateOnly Date, string Title, ItemStatus Status, DateTime? CreatedOn);

public record TodoItemDetailDto(Guid Id, DateOnly Date, string Title, ItemStatus Status, DateTime? CreatedOn);