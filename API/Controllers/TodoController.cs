using Core.DTOs;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

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
        public IEnumerable<TodoItemEntity> GetTodoItems()
        {
            return _todoService.GetAll();
        }

        [HttpGet("{id}")]
        public TodoItemEntity GetTodo(Guid id)
        {
            return _todoService.Get(id);
        }

        [HttpPost]
        public void CreateTodo(TodoItemDTO todo)
        {
            _todoService.Save(todo);
        }

        [HttpPut("{id}")]
        public void UpdateTodo(Guid id, TodoItemDTO todo)
        {
            _todoService.Update(id, todo);
        }

        [HttpDelete("{id}")]
        public void DeleteTodo(Guid id)
        {
            _todoService.Delete(id);
        }
    }
}
