using Core.Contexts;
using Core.Entities;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Business.Services;

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

public interface ITodoService
{
    Task<IEnumerable<TodoItemDetailResponse>> GetTodosAsync();
    Task<TodoItemDetailResponse> GetTodoAsync(Guid id);
    Task CreateTodoAsync(CreateTodoRequest todoItem);
    Task UpdateTodoAsync(Guid id, CreateTodoRequest todoItem);
    Task DeleteTodoAsync(Guid id);
}

public record CreateTodoRequest([Required] DateOnly Date, [Required] string Title, ItemStatus Status);

public record TodoItemDetailResponse(Guid Id, DateOnly Date, string Title, ItemStatus Status, DateTime? CreatedOn, string? CreatedBy, DateTime? UpdatedOn, string? UpdatedBy)
{
    public TodoItemDetailResponse(TodoItemEntity todo) : this(todo.Id, todo.Date, todo.Title, todo.Status, todo.CreatedOn, todo.CreatedBy, todo.UpdatedOn, todo.UpdatedBy)
    {
    }
}