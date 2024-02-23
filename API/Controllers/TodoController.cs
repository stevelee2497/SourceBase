using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Services.Todo;

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
        public IEnumerable<TodoItemEntity> Get()
        {
            return _todoService.Get();
        }
    }
}
