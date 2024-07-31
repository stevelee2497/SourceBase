using Core.Entities;
using System.Text.Json.Serialization;

namespace Core.DTOs
{
    public class TodoItemDto
    {
        public DateOnly Date { get; set; }

        public required string Title { get; set; }

        public ItemStatus Status { get; set; }
    }

    public class TodoItemDetailDto : TodoItemDto
    {
        [JsonPropertyOrder(-1)]
        public Guid Id { get; set; }
    }
}
