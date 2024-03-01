using Core.DbContexts;
using Core.DTOs;
using Core.Entities;

namespace Services
{
    public interface ITodoService
    {
        IEnumerable<TodoItemEntity> GetAll();

        TodoItemEntity Get(Guid id);

        void Save(TodoItemDTO todoItem);

        void Update(Guid id, TodoItemDTO todoItem);

        void Delete(Guid id);
    }

    public class TodoService : ITodoService
    {
        private readonly ApplicationDbContext _context;

        public TodoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public TodoItemEntity Get(Guid id)
        {
            return _context.TodoItems.Find(id);
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

        public void Update(Guid id, TodoItemDTO todoItem)
        {
            var item = _context.TodoItems.Find(id);
            item.Title = todoItem.Title;
            item.Status = todoItem.Status;
            item.Date = todoItem.Date;
            _context.SaveChanges();
        }

        public void Delete(Guid id)
        {
            var item = _context.TodoItems.Find(id);
            _context.TodoItems.Remove(item);
            _context.SaveChanges();
        }
    }
}
