using Core.DTOs;

namespace Services.Todo
{
    public interface ITodoService
    {
        IEnumerable<TodoItemDetailDto> GetTodoItems();

        TodoItemDetailDto GetTodo(Guid id);

        void CreateTodo(TodoItemDto todoItem);

        void UpdateTodo(Guid id, TodoItemDto todoItem);

        void DeleteTodo(Guid id);
    }
}
