using Core.DbContexts;
using Core.DTOs;
using Core.Entities;
using Core.Exceptions;
using Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Services.Todo
{
    public class TodoService(IDbContext context) : ITodoService
    {
        public async Task<TodoItemDetailDto> GetTodo(Guid id)
        {
            var todo = await context.TodoItems.FindAsync(id);
            return todo?.ToDetailDto() ?? throw new NotFoundException();
        }

        public async Task<IEnumerable<TodoItemDetailDto>> GetTodoItems()
        {
            return await context.TodoItems.Where(x => x.UserId == context.CurrentUserId).Select(x => x.ToDetailDto()).ToListAsync();
        }

        public async Task CreateTodo(TodoItemDto todoItem)
        {
            context.TodoItems.Add(new TodoItemEntity { Title = todoItem.Title, Date = todoItem.Date, UserId = context.CurrentUserId ?? throw new UnAuthorizedException() });
            await context.SaveChangesAsync();
        }

        public async Task UpdateTodo(Guid id, TodoItemDto todoItem)
        {
            var item = context.TodoItems.Find(id) ?? throw new NotFoundException();
            item.Title = todoItem.Title;
            item.Status = todoItem.Status;
            item.Date = todoItem.Date;
            await context.SaveChangesAsync();
        }

        public async Task DeleteTodo(Guid id)
        {
            var item = context.TodoItems.Find(id) ?? throw new NotFoundException();
            context.TodoItems.Remove(item);
            await context.SaveChangesAsync();
        }
    }
}
