using Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Todo;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/todo")]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;

        public TodoController(ITodoService todoService)
        {
            _todoService = todoService;
        }

        [HttpGet]
        public IEnumerable<TodoItemDetailDto> GetTodoItems()
        {
            return _todoService.GetTodoItems();
        }

        [HttpGet("{id}")]
        public TodoItemDetailDto GetTodo(Guid id)
        {
            return _todoService.GetTodo(id);
        }

        [HttpPost]
        public void CreateTodo(TodoItemDto todo)
        {
            _todoService.CreateTodo(todo);
        }

        [HttpPut("{id}")]
        public void UpdateTodo(Guid id, TodoItemDto todo)
        {
            _todoService.UpdateTodo(id, todo);
        }

        [HttpDelete("{id}")]
        public void DeleteTodo(Guid id)
        {
            _todoService.DeleteTodo(id);
        }
    }
}
