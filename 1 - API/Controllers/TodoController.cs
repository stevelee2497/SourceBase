using Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Todo;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/todos")]
    public class TodoController(ITodoService todoService) : ControllerBase
    {
        [HttpGet]
        public async Task<IEnumerable<TodoItemDetailDto>> GetTodoItems()
        {
            return await todoService.GetTodoItems();
        }

        [HttpGet("{id}")]
        public async Task<TodoItemDetailDto> GetTodo(Guid id)
        {
            return await todoService.GetTodo(id);
        }

        [HttpPost]
        public async Task CreateTodo(TodoItemDto todo)
        {
            await todoService.CreateTodo(todo);
        }

        [HttpPut("{id}")]
        public async Task UpdateTodo(Guid id, TodoItemDto todo)
        {
            await todoService.UpdateTodo(id, todo);
        }

        [HttpDelete("{id}")]
        public async Task DeleteTodo(Guid id)
        {
            await todoService.DeleteTodo(id);
        }
    }
}
