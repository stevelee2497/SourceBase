using Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Todo;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/todo")]
    public class TodoController(ITodoService todoService) : ControllerBase
    {
        [HttpGet]
        public IEnumerable<TodoItemDetailDto> GetTodoItems()
        {
            return todoService.GetTodoItems();
        }

        [HttpGet("{id}")]
        public TodoItemDetailDto GetTodo(Guid id)
        {
            return todoService.GetTodo(id);
        }

        [HttpPost]
        public void CreateTodo(TodoItemDto todo)
        {
            todoService.CreateTodo(todo);
        }

        [HttpPut("{id}")]
        public void UpdateTodo(Guid id, TodoItemDto todo)
        {
            todoService.UpdateTodo(id, todo);
        }

        [HttpDelete("{id}")]
        public void DeleteTodo(Guid id)
        {
            todoService.DeleteTodo(id);
        }
    }
}
