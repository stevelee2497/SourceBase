using Core.Entities;

namespace Services.Todo
{
    public class TodoService : ITodoService
    {
        public IEnumerable<TodoItemEntity> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new TodoItemEntity
            {
                Id = Guid.NewGuid(),
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Title = $"Task number {Random.Shared.Next(1, 55)}",
            })
            .ToArray();
        }
    }
}
