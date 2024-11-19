using Core.Contexts;
using Core.DTOs;
using Core.Entities;
using Core.Exceptions;
using Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Services.Todo
{
    public class TodoService(IDbContext dbContext) : ITodoService
    {
        public async Task<TodoItemDetailDto> GetTodoAsync(Guid id)
        {
            var todo = await dbContext.TodoItems.FindAsync(id);
            return todo?.ToDetailDto() ?? throw new NotFoundException();
        }

        public async Task<IEnumerable<TodoItemDetailDto>> GetTodoItemsAsync()
        {
            return await dbContext.TodoItems.Where(x => x.UserId == dbContext.GetCurrentUserId()).Select(x => x.ToDetailDto()).ToListAsync();
        }

        public async Task CreateTodoAsync(TodoItemDto todoItem)
        {
            dbContext.TodoItems.Add(new TodoItemEntity { Title = todoItem.Title, Date = todoItem.Date, UserId = dbContext.GetCurrentUserId() ?? throw new UnAuthorizedException() });
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
}
