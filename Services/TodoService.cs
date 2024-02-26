using Core.DbContexts;
using Core.Entities;

namespace Services
{
    public interface ITodoService
    {
        IEnumerable<TodoItemEntity> GetAll();
        void Save(string title, DateOnly date);
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

        public void Save(string title, DateOnly date)
        {
            _context.TodoItems.Add(new TodoItemEntity { Title = title, Date = date });
            _context.SaveChanges();
        }
    }
}
