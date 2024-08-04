using Core.DbContexts;
using Core.DTOs;
using Core.Entities;
using Core.Exceptions;
using Core.Extensions;

namespace Services.Todo
{
    public class TodoService : ITodoService
    {
        private readonly IDbContext _context;

        public TodoService(IDbContext context)
        {
            _context = context;
        }

        public TodoItemDetailDto GetTodo(Guid id)
        {
            return _context.TodoItems.Find(id)?.ToDetailDto() ?? throw new NotFoundException();
        }

        public IEnumerable<TodoItemDetailDto> GetTodoItems()
        {
            return _context.TodoItems.Where(x => x.UserId == _context.CurrentUserId).Select(x => x.ToDetailDto()).AsEnumerable();
        }

        public async Task CreateTodo(TodoItemDto todoItem)
        {
            _context.TodoItems.Add(new TodoItemEntity { Title = todoItem.Title, Date = todoItem.Date, UserId = _context.CurrentUserId });
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTodo(Guid id, TodoItemDto todoItem)
        {
            var item = _context.TodoItems.Find(id) ?? throw new NotFoundException();
            item.Title = todoItem.Title;
            item.Status = todoItem.Status;
            item.Date = todoItem.Date;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTodo(Guid id)
        {
            var item = _context.TodoItems.Find(id) ?? throw new NotFoundException();
            _context.TodoItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
