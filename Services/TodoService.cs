using Core.DbContexts;
using Core.DTOs;
using Core.Entities;

namespace Services
{
    public interface ITodoService
    {
        IEnumerable<TodoItemEntity> GetAll();
        void Save(TodoItemDTO todoItem);
    }

    public class TodoService : ITodoService
    {
        private readonly ApplicationDbContext _context;

        public TodoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TodoItemEntity> GetAll()
        {
            return _context.TodoItems.AsEnumerable();
        }

        public void Save(TodoItemDTO todoItem)
        {
            _context.TodoItems.Add(new TodoItemEntity { Title = todoItem.Title, Date = todoItem.Date });
            _context.SaveChanges();
        }
    }
}
