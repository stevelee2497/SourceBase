using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TodoController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<TodoController> _logger;

        public TodoController(ILogger<TodoController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<TodoItem> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new TodoItem
            {
                Id = Guid.NewGuid(),
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Title = $"Task number {Random.Shared.Next(1, 55)}",
            })
            .ToArray();
        }
    }
}
