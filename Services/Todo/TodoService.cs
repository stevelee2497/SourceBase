using Core.DbContexts;
using Core.DTOs;
using Core.Entities;
using Core.Exceptions;
using Core.Extensions;
using Core.Helpers;

namespace Services.Todo
{
    public class TodoService : ITodoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISessionUserHelper _sessionUserHelper;

        public TodoService(ApplicationDbContext context, ISessionUserHelper sessionUserHelper)
        {
            _context = context;
            _sessionUserHelper = sessionUserHelper;
        }

        public TodoItemDetailDto GetTodo(Guid id)
        {
            return _context.TodoItems.Find(id)?.ToDetailDto() ?? throw new SystemApiException("User not found");
        }

        public IEnumerable<TodoItemDetailDto> GetTodoItems()
        {
            return _context.TodoItems.Where(x => x.UserId == _sessionUserHelper.UserId).Select(x => x.ToDetailDto()).AsEnumerable();
        }

        public void CreateTodo(TodoItemDto todoItem)
        {
            _context.TodoItems.Add(new TodoItemEntity { Title = todoItem.Title, Date = todoItem.Date, UserId = _sessionUserHelper.UserId });
            _context.SaveChanges();
        }

        public void UpdateTodo(Guid id, TodoItemDto todoItem)
        {
            var item = _context.TodoItems.Find(id) ?? throw new SystemApiException("User not found");
            item.Title = todoItem.Title;
            item.Status = todoItem.Status;
            item.Date = todoItem.Date;
            _context.SaveChanges();
        }

        public void DeleteTodo(Guid id)
        {
            var item = _context.TodoItems.Find(id) ?? throw new SystemApiException("Item not found");
            _context.TodoItems.Remove(item);
            _context.SaveChanges();
        }
    }
}
