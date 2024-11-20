using Business.Interfaces;
using Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            return await todoService.GetTodoItemsAsync();
        }

        [HttpGet("{id}")]
        public async Task<TodoItemDetailDto> GetTodo(Guid id)
        {
            return await todoService.GetTodoAsync(id);
        }

        [HttpPost]
        public async Task CreateTodo(TodoItemDto todo)
        {
            await todoService.CreateTodoAsync(todo);
        }

        [HttpPut("{id}")]
        public async Task UpdateTodo(Guid id, TodoItemDto todo)
        {
            await todoService.UpdateTodoAsync(id, todo);
        }

        [HttpDelete("{id}")]
        public async Task DeleteTodo(Guid id)
        {
            await todoService.DeleteTodoAsync(id);
        }
    }
}
