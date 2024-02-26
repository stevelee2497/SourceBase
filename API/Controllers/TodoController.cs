using Core.DTOs;
using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
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

        [HttpPost]
        public void CreateTodo(TodoItemDTO todo)
        {
            _todoService.Save(todo.Title, todo.Date);
        }
    }
}
