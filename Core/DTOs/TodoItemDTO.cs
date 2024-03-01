using Core.Entities;

namespace Core.DTOs
{
    public class TodoItemDTO
    {
        public DateOnly Date { get; set; }

        public string Title { get; set; }

        public ItemStatus Status { get; set; }
    }
}
